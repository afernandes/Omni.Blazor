// Omni.Blazor speech recognition services — lazily imported ECMAScript module.
import { invokeApi } from './omni-module.js';

const ns = {};

  // ─── Speech-to-Text (Web Speech API) ──────────────────────────────────────
  // Wrapper sobre window.SpeechRecognition + webkit variant com state machine
  // explícito. Resolve 4 classes de bugs:
  //   1. Race conditions / double-click / InvalidStateError em Chrome
  //   2. UI mentindo "AO VIVO" quando Edge ainda está conectando ao serviço
  //   3. Auto-retry sobrescrevendo stop do usuário
  //   4. Stop preso (Edge não dispara onend após stop())
  //
  // Estados (espelhados em C# SpeechRecognitionState):
  //   idle       — sem sessão, pronto pra start
  //   connecting — clicou; aguardando audio capture (cobre onstart + retry loop)
  //   recording  — mic capturando de FATO (onaudiostart disparou)
  //   stopping   — usuário pediu stop, aguardando onend
  //   error      — falha não-recuperável; volta pra idle no próximo onend
  //
  // GATILHO CRÍTICO: a transição connecting → recording ocorre em onaudiostart,
  // NÃO em onstart. onstart só significa "browser criou o reconhecedor", não
  // "mic está capturando". No Edge especialmente, há uma janela onde onstart
  // já disparou mas o Microsoft Speech Service ainda está conectando.
  //
  // RETRY LOOP: erros de rede no Edge causam retry silencioso. Durante retry
  // o estado público FICA em 'connecting' (não pisca pra idle/recording).
  // O retry é cancelado se o usuário pedir stop (flag _speechUserStop).
  let _speechRec = null;
  let _speechState = 'idle';
  let _speechStartGuard = null;   // setTimeout id — watchdog do connecting
  let _speechPending = null;      // {dotnet, opts} aguardando 'end' atual
  let _speechLastToggleAt = 0;    // ms — debounce de cliques duplos
  let _speechRetryCount = 0;      // contador de retries por sessão (zera em onaudiostart)
  let _speechRetrying = false;    // true durante retry silencioso (mantém connecting)
  let _speechUserStop = false;    // true quando user pediu stop — bloqueia auto-retry
  const SPEECH_DEBOUNCE_MS = 250;

  // Detecção de Edge — UA contém "Edg/" (não "Edge/" que é o Edge legacy IE-based).
  // Edge usa Microsoft Speech Service, que tem cold start mais lento e é mais
  // sensível a rede.
  const _isEdge = (() => {
    try { return /\bEdg\//.test(navigator.userAgent); }
    catch { return false; }
  })();

  // Watchdog do connecting: tempo total esperado da chamada start() até
  // onaudiostart confirmar que o mic está ativo. Edge precisa de muito mais
  // tempo porque pode envolver retries do Microsoft Service.
  const SPEECH_CONNECT_TIMEOUT_MS = _isEdge ? 12000 : 5000;

  // Cooldown entre sessões (release de recurso de mic no browser).
  const SPEECH_PENDING_DELAY_MS = _isEdge ? 250 : 80;

  // Max retries automáticos por sessão pra erros transient (network).
  const SPEECH_MAX_RETRIES = _isEdge ? 2 : 0;

  // Map de string → int espelha SpeechRecognitionState C#.
  // ORDEM IMPORTANTE: deve casar com Models/SpeechRecognitionResult.cs.
  const _stateInt = { idle: 0, connecting: 1, recording: 2, stopping: 3, error: 4 };

  function _speechSetState(newState, dotnet, opts) {
    if (_speechState === newState) return;
    _speechState = newState;
    if (dotnet && opts && opts.stateMethod) {
      const stateIntVal = _stateInt[newState] ?? 0;
      try { dotnet.invokeMethodAsync(opts.stateMethod, stateIntVal); } catch { /* circuit gone */ }
    }
  }

  function _speechClearGuard() {
    if (_speechStartGuard) {
      clearTimeout(_speechStartGuard);
      _speechStartGuard = null;
    }
  }

  function _speechResetFlags() {
    _speechRetryCount = 0;
    _speechRetrying = false;
    _speechUserStop = false;
  }

  ns.speechSupported = function () {
    try { return !!(window.SpeechRecognition || window.webkitSpeechRecognition); }
    catch { return false; }
  };

  ns.speechState = function () { return _speechState; };

  /// Retorna info sobre o engine de reconhecimento (Chrome=Google, Edge=Microsoft).
  /// Permite UI customizar tooltip/hint pra avisar que Edge pode ser mais lento.
  ns.speechEngine = function () {
    if (_isEdge) return 'edge';
    try {
      if (/\bChrome\//.test(navigator.userAgent) && !/\bEdg\//.test(navigator.userAgent)) return 'chrome';
      if (/\bSafari\//.test(navigator.userAgent) && !/\bChrome\//.test(navigator.userAgent)) return 'safari';
    } catch { }
    return 'other';
  };

  ns.speechIsRecording = function (dotnet) {
    if (!_speechRec || _speechState !== 'recording') return false;
    if (!dotnet) return true;
    return _speechRec._dotnet && _speechRec._dotnet._id === dotnet._id;
  };

  ns.speechToggle = function (dotnet, opts) {
    if (!dotnet || !opts) return;
    const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!SR) {
      try { dotnet.invokeMethodAsync(opts.unsupportedMethod || 'OnUnsupported'); }
      catch { /* circuit gone */ }
      return;
    }

    // Debounce contra dupla-click rápida (causa InvalidStateError em Chrome).
    const now = Date.now();
    if (now - _speechLastToggleAt < SPEECH_DEBOUNCE_MS) return;
    _speechLastToggleAt = now;

    if (_speechRec) {
      const sameComponent = _speechRec._dotnet && _speechRec._dotnet._id === dotnet._id;

      if (sameComponent) {
        // Mesmo componente:
        //   connecting/recording → user quer parar (sinaliza intenção + cancela retry)
        //   stopping/error       → ignora (esperando cleanup)
        if (_speechState === 'connecting' || _speechState === 'recording') {
          _speechUserStop = true;
          _speechRetrying = false;
          _speechPending = null;          // cancela qualquer retry pendente
          _speechRetryCount = SPEECH_MAX_RETRIES; // bloqueia novos retries
          _speechSetState('stopping', dotnet, opts);
          const target = _speechRec;
          try { target.stop(); } catch { try { target.abort(); } catch { } }
          // Edge: stop() às vezes não dispara onend; força abort após 1.5s
          if (_isEdge) {
            setTimeout(() => {
              if (_speechRec === target && _speechState === 'stopping') {
                try { target.abort(); } catch { }
              }
            }, 1500);
          }
        }
        return;
      }

      // Outro componente requisitou start. Enfileira pending e para o atual.
      // Se já havia outro pending (3+ cliques), avisa o displaced.
      if (_speechPending) {
        const displaced = _speechPending;
        try { displaced.dotnet.invokeMethodAsync(displaced.opts.errorMethod || 'OnError', 'superseded'); } catch { }
        try { displaced.dotnet.invokeMethodAsync(displaced.opts.stateMethod || 'OnStateChange', 0); } catch { }
      }
      _speechPending = { dotnet, opts };
      if (_speechState === 'recording' || _speechState === 'connecting') {
        const curDotnet = _speechRec._dotnet;
        const curOpts = _speechRec._opts;
        _speechUserStop = false; // cross-component não é user-stop
        _speechRetrying = false;
        _speechSetState('stopping', curDotnet, curOpts);
        try { _speechRec.stop(); } catch { try { _speechRec.abort(); } catch { } }
      }
      return;
    }

    // Sem instância ativa → inicia nova (limpa flags antes).
    _speechResetFlags();
    _speechStartNew(dotnet, opts, SR);
  };

  function _speechStartNew(dotnet, opts, SR) {
    let r;
    try {
      r = new SR();
    } catch (err) {
      _speechSetState('idle', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', 'ctor-failed'); } catch { }
      return;
    }
    r._dotnet = dotnet;
    r._opts = opts;
    r.continuous = opts.continuous ?? true;
    r.interimResults = opts.interimResults ?? false;
    r.maxAlternatives = Math.max(1, opts.maxAlternatives ?? 1);
    if (opts.language) r.lang = opts.language;

    // onstart: browser criou o recognizer. NÃO É AINDA "recording" — apenas
    // confirma que start() foi aceito. Mantém em 'connecting' até onaudiostart.
    r.onstart = () => {
      if (_speechState !== 'connecting') {
        _speechSetState('connecting', dotnet, opts);
      }
    };

    // onaudiostart: mic CAPTURANDO de fato. Esse é o gatilho real do Recording.
    // Limpa o watchdog porque chegamos no destino. Notifica OnStart (callback
    // user-facing que significa "estou ouvindo agora").
    r.onaudiostart = () => {
      _speechClearGuard();
      _speechResetFlags();
      _speechSetState('recording', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.startMethod || 'OnStart'); } catch { }
    };

    r.onresult = (e) => {
      if (!e.results || e.results.length === 0) return;
      const last = e.results[e.results.length - 1];
      const alt = last[0];
      try {
        dotnet.invokeMethodAsync(opts.resultMethod || 'OnResult',
          alt.transcript, last.isFinal, alt.confidence || 0);
      } catch { }
    };

    r.onerror = (e) => {
      const code = e.error || 'unknown';
      // Erros esperados que não devem virar 'error' state:
      //   no-speech — silêncio longo (Chrome/Edge dispara após ~10s sem fala)
      //   aborted   — stop programático (esperado, parte do fluxo normal)
      const isFatal = code !== 'no-speech' && code !== 'aborted';

      // Auto-retry pra erros transient no Edge (Microsoft Service é flaky).
      // Condições: erro retryable + ainda temos retries + user NÃO pediu stop.
      const isRetryableNet = (code === 'network' || code === 'audio-capture');
      if (isRetryableNet && _speechRetryCount < SPEECH_MAX_RETRIES && !_speechUserStop) {
        _speechRetryCount++;
        _speechRetrying = true;           // bloqueia idle no finalize
        _speechPending = { dotnet, opts };// agenda restart
        // Não notifica error pro C#, não muda state — fica em 'connecting'
        return;
      }

      _speechClearGuard();
      if (isFatal) _speechSetState('error', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', code); } catch { }
    };

    // Cleanup compartilhado: chamado em onend real OU watchdog defensivo.
    let _ended = false;
    const finalize = () => {
      if (_ended) return;
      _ended = true;
      _speechClearGuard();
      if (_speechRec === r) _speechRec = null;

      // CASO 1: User pediu stop — força idle, NÃO processa pending de retry.
      if (_speechUserStop) {
        _speechSetState('idle', dotnet, opts);
        try { dotnet.invokeMethodAsync(opts.endMethod || 'OnEnd'); } catch { }
        _speechResetFlags();
        _speechPending = null;
        return;
      }

      // CASO 2: Retry em andamento — MANTÉM em connecting, NÃO notifica OnEnd.
      // Estado público continua estável; só dispara o restart silencioso.
      if (_speechRetrying && _speechPending) {
        const pending = _speechPending;
        _speechPending = null;
        // Estado fica em 'connecting' (transição feita no onerror foi none)
        setTimeout(() => {
          _speechLastToggleAt = 0;
          ns.speechToggle(pending.dotnet, pending.opts);
        }, SPEECH_PENDING_DELAY_MS);
        return;
      }

      // CASO 3: Fim normal — vai pra idle + notifica OnEnd.
      _speechSetState('idle', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.endMethod || 'OnEnd'); } catch { }

      // Pending de OUTRO componente (cross-component switch)? Despacha.
      if (_speechPending && _speechRec === null) {
        const pending = _speechPending;
        _speechPending = null;
        setTimeout(() => {
          _speechLastToggleAt = 0;
          ns.speechToggle(pending.dotnet, pending.opts);
        }, SPEECH_PENDING_DELAY_MS);
      }
    };

    r.onend = () => {
      if (_speechRec !== r && _ended) return;
      finalize();
    };

    _speechRec = r;
    // Vai DIRETO pra 'connecting' (sem estado intermediário 'starting').
    // Se já estávamos em 'connecting' (retry path), no-op via guard interno.
    if (_speechState !== 'connecting') {
      _speechSetState('connecting', dotnet, opts);
    }

    // Watchdog: se onaudiostart não dispara em SPEECH_CONNECT_TIMEOUT_MS,
    // o reconhecedor está travado (permissão pendente, audio device sumiu,
    // ou Microsoft Service inalcançável após retries).
    _speechStartGuard = setTimeout(() => {
      if (_speechState !== 'connecting' || _speechRec !== r) return;
      _speechSetState('error', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', 'connect-timeout'); } catch { }
      try { r.abort(); } catch { /* abort dispara onend */ }
      setTimeout(() => { if (_ended) return; finalize(); }, 500);
    }, SPEECH_CONNECT_TIMEOUT_MS);

    try {
      r.start();
    } catch (err) {
      _speechClearGuard();
      _speechRec = null;
      const code = (err && err.name === 'InvalidStateError') ? 'invalid-state' : (err && err.name) || String(err);
      _speechSetState('error', dotnet, opts);
      try { dotnet.invokeMethodAsync(opts.errorMethod || 'OnError', code); } catch { }
      setTimeout(() => {
        if (_speechRec === null && _speechState === 'error') {
          _speechSetState('idle', dotnet, opts);
        }
      }, 200);
    }
  }

  ns.speechStop = function (dotnet) {
    if (!_speechRec) return;
    if (dotnet && _speechRec._dotnet && _speechRec._dotnet._id !== dotnet._id) return;
    if (_speechState === 'recording' || _speechState === 'connecting') {
      _speechUserStop = true;
      _speechRetrying = false;
      _speechPending = null;
      _speechRetryCount = SPEECH_MAX_RETRIES;
      _speechSetState('stopping', _speechRec._dotnet, _speechRec._opts);
    }
    const target = _speechRec;
    try { target.stop(); } catch { try { target.abort(); } catch { } }

    if (_isEdge) {
      setTimeout(() => {
        if (_speechRec === target && _speechState === 'stopping') {
          try { target.abort(); } catch { }
        }
      }, 1500);
    }
  };


export function invoke(identifier, args) {
  return invokeApi(ns, identifier, args);
}
