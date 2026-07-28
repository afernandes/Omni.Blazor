# Omni.Templates

Páginas prontas montadas com os componentes da **Omni.Blazor** — telas que todo sistema
precisa (login, dashboard, tabela de usuários, erros…) para você **copiar, colar e ajustar**.

> **Isto não é um pacote NuGet nem um `dotnet new`.** São *starters de copy-paste*: cada
> página tem um botão **"Ver código"** com o markup pronto. Você copia o que precisa
> direto para o seu projeto — sem herdar dependência, sem acoplamento com esta galeria.

## Como visualizar

A galeria é um app Blazor Server local:

```bash
dotnet run --project src/Omni.Templates/Omni.Templates.Host
```

Abre em **http://localhost:5305**. Alterne claro/escuro e a cor de destaque pelo seletor
de tema no topo — os templates usam apenas tokens `--omni-*`, então acompanham qualquer tema.

Para o preview via ferramentas (`preview_*`), o servidor está registrado em
`.claude/launch.json` como **`templates`**.

## O que tem

**26 páginas** em 8 categorias. Rotas relativas à raiz da galeria.

### Frame do app

| Template | Rota | Componentes-chave |
|---|---|---|
| **App shell** (topbar + sidebar) | `/templates/shell` | `OmniLayout`, `OmniAppBar`, `OmniDrawer`, `OmniDrawerToggle`, `OmniPanelMenuSection`, `OmniMain` |

> O app shell é o artefato mais reaproveitável da galeria: é o frame externo que todo app
> precisa. Ele tem um link **"Tela cheia"** (`/templates/shell/full`) que o mostra sem o
> chrome da galeria — é assim que ele vai parecer no seu app.

### Autenticação

| Template | Rota | Componentes-chave |
|---|---|---|
| Login | `/templates/auth/login` | `OmniAuthLayout`, `OmniFormField`, `OmniPassword`, `OmniSocialButton` |
| Cadastro | `/templates/auth/register` | `OmniAuthLayout`, `OmniPasswordStrength`, `OmniCheckBox` |
| Recuperar senha | `/templates/auth/forgot` | `OmniAuthLayout`, `OmniTextBox`, `OmniResult` |
| Redefinir senha | `/templates/auth/reset` | `OmniPassword`, `OmniPasswordStrength` |
| Verificação 2FA | `/templates/auth/2fa` | `OmniSecurityCode` |
| Confirmar e-mail | `/templates/auth/email-confirm` | `OmniResult` |
| Consentimento OAuth | `/templates/auth/consent` | `OmniAuthLayout`, `OmniIcon` |
| Conta bloqueada | `/templates/auth/lockout` | `OmniResult` |
| Sessão expirada | `/templates/auth/session-expired` | `OmniResult` |

### Admin & conta

| Template | Rota | Componentes-chave |
|---|---|---|
| Tabela de usuários | `/templates/admin/users` | `OmniDataGrid`, `OmniStatusBadge`, `OmniPaneHeader` |
| Papéis (roles) | `/templates/admin/roles` | `OmniDataGrid`, `OmniAvatarGroup` |
| Permissões | `/templates/admin/permissions` | `OmniTree`, `OmniTreeLevel` |
| Sessões ativas | `/templates/admin/sessions` | `OmniDataGrid`, `OmniAvatar` |
| Logs de auditoria | `/templates/admin/audit` | `OmniDataGrid`, `OmniStatusBadge` |
| Configurações | `/templates/admin/settings` | `OmniCard`, `OmniDescriptionList`, `OmniSwitch` |
| Perfil & conta | `/templates/account/profile` | `OmniAvatar`, `OmniDescriptionList` |

### Dashboard, marketing & estados

| Template | Rota | Componentes-chave |
|---|---|---|
| Dashboard | `/templates/dashboard` | `OmniStat`, `OmniChart`, `OmniCard` |
| Planos (pricing) | `/templates/pricing` | `OmniOptionCard` |
| Estado vazio | `/templates/misc/empty` | `OmniEmptyState` |
| Banners & avisos | `/templates/misc/banners` | `OmniBanner` |

### Erros

| Template | Rota | Componentes-chave |
|---|---|---|
| 404 — Não encontrado | `/templates/error/404` | `OmniResult` |
| 403 — Acesso negado | `/templates/error/403` | `OmniResult` |
| 500 — Erro interno | `/templates/error/500` | `OmniResult` |
| Manutenção | `/templates/error/maintenance` | `OmniResult` |

## Como usar

1. Rode a galeria e abra a página que quer.
2. Clique em **"Ver código"** → **"Copiar"**.
3. Cole no seu projeto e ajuste (dados, rotas, textos).

O código copiável é **autossuficiente**: usa apenas componentes `Omni*` e classes CSS que
**shipam no pacote** (`_content/Omni.Blazor/css/omni.css`) ou estilos inline. Ele nunca
depende do CSS da galeria — isso é verificado automaticamente na CI por
[`scripts/check_template_code.py`](../../scripts/check_template_code.py), que falha o build
se algum bloco copiável referenciar uma classe que o consumidor não teria.

### Ressalvas

- **Dados são ilustrativos.** Nomes, valores e e-mails nos exemplos são fictícios — troque pelos seus.
- **Textos em pt-BR.** As páginas foram escritas em português; traduza conforme o seu produto.
- **Sem lógica de backend.** São telas: os formulários não postam, os botões não navegam
  (exceto os fluxos de auth entre si). A intenção é o *layout*, não o comportamento.

## Estrutura

```
src/Omni.Templates/
├── Omni.Templates/            # RCL com as páginas (referenciável, mas pensada p/ copy-paste)
│   ├── Layout/
│   │   ├── TemplatesLayout.razor   # chrome da galeria (nav + topbar)
│   │   └── BlankLayout.razor       # sem chrome — usado pelas views "Tela cheia"
│   ├── Pages/Templates/<Categoria>/*.razor
│   ├── Shared/
│   │   ├── TemplateDoc.razor       # wrapper: título + preview + código copiável
│   │   └── AppShell.razor          # o frame do app (preview e tela cheia usam o mesmo)
│   └── Themes/templates.scss       # estilos `tpl-*` da galeria (NÃO vão no código copiável)
└── Omni.Templates.Host/       # host Blazor Server só para o preview (porta 5305)
```
