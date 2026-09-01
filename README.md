# Skynet API

API de autenticação em .NET 10, com login/registro de usuários, emissão de JWT assinado com **RSA** (chave privada/pública), refresh token (rotativo, via cookie HttpOnly) e blacklist de tokens revogados.

## Arquitetura

Solução organizada em Clean Architecture, definida em `Skynet.slnx`:

- **Skynet.Domain** — entidades, interfaces e settings (sem dependências externas).
- **Skynet.Application** — casos de uso / serviços (`AuthService`, `JwtService`, DTOs de auth).
- **Skynet.Infra** — EF Core (MySQL via Pomelo), repositórios, geração de tokens (`AccessTokenGenerator`, `RefreshTokenGenerator`), hashing de senha.
- **Skynet.API** — Web API (controllers, autenticação JWT, versionamento, Swagger, CORS).
- **Tests** — testes de cada camada (`Skynet.API.Tests`, `Skynet.Application.Tests`, `Skynet.Domain.Tests`, `Skynet.Infra.Tests`).

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- MySQL (local ou remoto) acessível pela connection string configurada
- OpenSSL (ou similar) para gerar o par de chaves RSA usado no JWT

## Configuração (dotnet user-secrets)

Este projeto **não** guarda segredos em `appsettings*.json`. Tudo o que for sensível deve ir para o `dotnet user-secrets`, escopado ao projeto `Skynet.API` (`UserSecretsId` já definido no `.csproj`).

Segredos que você precisa configurar antes de rodar a API:

| Chave | Descrição | Obrigatório |
|---|---|---|
| `ConnectionStrings:SkynetDB` | Connection string do MySQL | Sim |
| `Jwt:PublicKey` | Chave pública RSA (PEM), usada para **validar** o token | Sim |
| `Jwt:PrivateKey` | Chave privada RSA (PEM), usada para **assinar** o token | Sim |
| `Jwt:Issuer` | Emissor do token (default: `skynet-api-auth`) | Não |
| `Jwt:Audience` | Audiência do token (default: `skynet-api-auth`) | Não |
| `Jwt:AccessTokenExpirationMinutes` | Expiração do access token em minutos (default: `15`) | Não |
| `Jwt:RefreshTokenExpirationDays` | Expiração do refresh token em dias (default: `7`) | Não |
| `Jwt:MaxActiveSessionsPerUser` | Sessões ativas simultâneas por usuário (default: `5`) | Não |

A API valida no startup (`app.ValidateJwtSettings()`) que `Jwt:PublicKey` existe e é um PEM válido — se faltar ou for inválida, a aplicação falha ao subir.

### Gerando o par de chaves RSA

```bash
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out private.pem
openssl rsa -pubout -in private.pem -out public.pem
```

### Definindo os secrets

```bash
cd Skynet.API

dotnet user-secrets set "ConnectionStrings:SkynetDB" "Server=localhost;Port=3306;Database=skynet;User=root;Password=SUASENHA;"

dotnet user-secrets set "Jwt:PrivateKey" "$(cat ../private.pem)"
dotnet user-secrets set "Jwt:PublicKey" "$(cat ../public.pem)"
```

> No PowerShell, use `Get-Content ..\private.pem -Raw` no lugar de `cat`.

Para conferir o que está configurado (sem expor os valores no terminal/logs compartilhados):

```bash
dotnet user-secrets list --project Skynet.API
```

## Rodando o projeto

```bash
dotnet restore
dotnet ef database update --project Skynet.Infra --startup-project Skynet.API
dotnet run --project Skynet.API
```

Em ambiente de desenvolvimento, o Swagger fica disponível na raiz da aplicação.

## Testes

```bash
dotnet test
```

## Endpoints de autenticação

Base route: `api/v1/auth-service`

| Método | Rota | Descrição |
|---|---|---|
| `POST` | `/login` | Autentica o usuário; retorna access token no corpo e refresh token em cookie HttpOnly |
| `POST` | `/register` | Cria um novo usuário |
| `POST` | `/refresh` | Troca o refresh token (cookie) por um novo par de tokens |
| `DELETE` | `/logout` | Revoga o access token atual (blacklist) e o refresh token associado — requer `Authorize` |

## CORS

As origens permitidas são configuradas via `Cors:AllowedOrigins` (array de strings) em `appsettings.json`/`appsettings.Development.json` — não é um segredo, pode ficar versionado. Sem essa seção, a policy libera qualquer origem.
