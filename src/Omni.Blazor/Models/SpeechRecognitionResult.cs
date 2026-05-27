namespace Omni.Blazor.Models;

/// <summary>
/// Resultado de uma rodada de reconhecimento de voz disparado pelo
/// <c>OmniSpeechToText</c>. Mapeia diretamente o <c>SpeechRecognitionResult</c>
/// da Web Speech API: cada <c>onresult</c> emite uma instância dessa record.
/// </summary>
/// <param name="Transcript">Texto reconhecido (best alternative, <c>alternatives[0]</c>).</param>
/// <param name="IsFinal">
/// <c>true</c> quando esse é o trecho final pra essa "rodada de fala" — o motor
/// não vai mais alterá-lo. <c>false</c> = resultado interim/rascunho (só dispara
/// se <c>InterimResults=true</c> no componente). Use pra render: cinza enquanto
/// interim, preto quando final.
/// </param>
/// <param name="Confidence">
/// Score 0..1 de confiança da transcrição. Muitos browsers retornam 0 pra
/// resultados interim. Use pra filtrar transcrições ruins (ex: ignore &lt; 0.6).
/// </param>
public readonly record struct SpeechRecognitionResult(
    string Transcript,
    bool IsFinal,
    double Confidence);

/// <summary>
/// Estados de uma sessão de reconhecimento de voz. Reflete a state machine
/// interna do JS pra evitar race conditions e dar feedback visual preciso
/// na UI (idle, conectando, gravando, parando, erro).
/// </summary>
public enum SpeechRecognitionState
{
    /// <summary>Sem sessão ativa. Pronto pra começar.</summary>
    Idle = 0,
    /// <summary>Reconhecedor iniciado mas mic AINDA NÃO está capturando.
    /// Cobre o intervalo entre <c>start()</c> e o evento <c>onaudiostart</c>,
    /// incluindo retries silenciosos no Edge (Microsoft Speech Service).
    /// Botão deve mostrar "Conectando…" — JAMAIS "Ouvindo" nesse estado.</summary>
    Connecting = 1,
    /// <summary>Mic capturando áudio de fato (<c>onaudiostart</c> disparou).
    /// É o estado verdadeiro de "AO VIVO" — só aqui o usuário pode falar
    /// e ser ouvido. Botão mostra "Ouvindo…" + halo pulsante.</summary>
    Recording = 2,
    /// <summary><c>stop()</c> chamado pelo usuário, aguardando <c>onend</c>.
    /// Botão indica transição (spinner suave). Cancela auto-retry pendente.</summary>
    Stopping = 3,
    /// <summary>Erro não-recuperável — permissão negada, connect-timeout,
    /// audio-capture irrecuperável. Auto-volta pra <c>Idle</c> no próximo
    /// <c>onend</c> ou em 200ms.</summary>
    Error = 4,
}
