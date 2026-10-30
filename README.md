# CP6 - Catálogo de Jogos

Checkpoint 6 de .NET: Web API para gerenciar jogos de videogame, com CRUD em memória, arquitetura em camadas, autenticação JWT, Scalar e consultas LINQ. Implementação baseada no PDF **CP6 - CRUD e JWT** fornecido na atividade.

## Tecnologias

- C# e ASP.NET Core Web API, com Controllers e .NET 8.
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.33.
- Scalar.AspNetCore 2.6.6 como interface interativa.
- Swashbuckle SwaggerGen/Swagger 6.6.2 para gerar e servir o documento OpenAPI. Não há Swagger UI.
- LINQ, injeção de dependência nativa e `List<Jogo>`.

## Execução

Instale o **SDK do .NET 8** (o runtime sozinho não basta) e tenha acesso ao NuGet na primeira restauração. Não é necessário banco, Docker ou configuração de certificado HTTPS para o perfil HTTP local.

Depois de extrair o ZIP, abra o terminal na pasta `CP6_CatalogoJogos`:

```bash
dotnet --version
dotnet restore
dotnet build
dotnet run --launch-profile http
```

- API local: `http://localhost:5080/api/jogos`.
- Scalar: [http://localhost:5080/scalar/v1](http://localhost:5080/scalar/v1).
- OpenAPI: [http://localhost:5080/openapi/v1.json](http://localhost:5080/openapi/v1.json).

O perfil `http` ativa `Development`. O Scalar e o documento OpenAPI estão disponíveis apenas nesse ambiente. Acessar `/api/jogos` diretamente sem token deve retornar **401**.

## Login e JWT no Scalar

1. Abra o Scalar e selecione `POST /api/auth/login`.
2. Abra o cliente de teste da operação ("Test Request"). Envie este JSON:

```json
{
  "username": "Mauricio",
  "password": "123456"
}
```

3. Copie o valor do campo `token` da resposta 200.
4. Na configuração de autenticação da interface/cliente, selecione o esquema **Bearer**. Cole somente o token, sem escrever `Bearer` antes dele.
5. Execute `GET /api/jogos` e confira a resposta 200.
6. Remova o token e repita a requisição para conferir o 401. Repita com um token inválido.

O login é público, marcado com `[AllowAnonymous]`. Todos os endpoints de jogos herdam `[Authorize]` do controller. O filtro `BearerSecurityOperationFilter` adiciona o requisito Bearer somente às operações protegidas do documento OpenAPI; ele cumpre o papel do transformer de segurança usando o gerador compatível com .NET 8.

O token inclui `sub`, `jti`, `iss`, `aud`, `nbf` e `exp`; usa HS256 com chave simétrica e dura 3.600 segundos. A validação confere assinatura, algoritmo, emissor, destinatário e validade, sem tolerância após a expiração (`ClockSkew = TimeSpan.Zero`).

As configurações são lidas de `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` e `Jwt:ExpirationSeconds`. A chave pública de exemplo e as credenciais fixas são somente para demonstração acadêmica. Não use esses valores em produção.

Exemplo equivalente de login via terminal:

```bash
curl -i -X POST http://localhost:5080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"Mauricio","password":"123456"}'
```

Na requisição HTTP, ao contrário do campo de token do Scalar, use o prefixo `Bearer` no cabeçalho:

```bash
curl -i http://localhost:5080/api/jogos \
  -H 'Authorization: Bearer COLE_O_TOKEN_AQUI'
```

## Endpoints

| Método | Rota | JWT | Resposta de sucesso |
| --- | --- | --- | --- |
| POST | `/api/auth/login` | Não | 200 com token, tipo e expiração |
| POST | `/api/jogos` | Sim | 201 com jogo e cabeçalho Location |
| GET | `/api/jogos` | Sim | 200 com lista |
| GET | `/api/jogos/{id}` | Sim | 200 com jogo |
| PUT | `/api/jogos/{id}` | Sim | 200 com jogo atualizado |
| DELETE | `/api/jogos/{id}` | Sim | 204 sem corpo |
| GET | `/api/jogos/busca?plataforma=PC&precoMaximo=100` | Sim | 200 com lista filtrada |
| GET | `/api/jogos/relatorio-estoque` | Sim | 200 com relatório por plataforma |

Erros: **400** para dados inválidos, **401** para credenciais/token inválidos ou token ausente/expirado e **404** para ID inexistente. As rotas de ID usam `{id:int}`, evitando conflito com `busca` e `relatorio-estoque`.

## Cadastro e atualização

O mesmo corpo serve para POST e PUT:

```json
{
  "titulo": "Hollow Knight",
  "plataforma": "PC",
  "genero": "Aventura",
  "preco": 46.99,
  "anoLancamento": 2017,
  "estoque": 5
}
```

O ID é gerado no repositório, de forma incremental. Na atualização, o ID vem da rota e permanece o mesmo. PUT substitui os dados do jogo. Título, plataforma e gênero não aceitam texto vazio; preço e estoque não aceitam valores negativos. O ano deve estar entre 1950 e o ano seguinte ao atual. Preço e estoque iguais a zero são permitidos. Os campos de texto são aparados antes do armazenamento.

## Filtro e relatório

Exemplo de filtro:

```text
GET /api/jogos/busca?plataforma=PlayStation%205&precoMaximo=300
```

Os dois parâmetros são obrigatórios. O filtro utiliza `Where`, compara plataforma sem diferenciar maiúsculas/minúsculas e inclui o preço máximo. Sem correspondências, retorna `200` com `[]`.

O relatório utiliza `GroupBy`, `Select`, `Count` e `Sum`, calculando **soma do preço multiplicado pelo estoque**. A quantidade de títulos conta cadastros, não unidades em estoque. Plataformas com diferença apenas de maiúsculas/minúsculas ficam no mesmo grupo.

Exemplo de um item do relatório com os dados iniciais:

```json
{
  "plataforma": "PlayStation 5",
  "quantidadeTitulos": 1,
  "valorTotalEstoque": 1249.50
}
```

O endpoint retorna uma lista de itens como esse. A listagem inicial tem cinco jogos e cinco plataformas. Se o catálogo ficar vazio, lista, filtro e relatório retornam listas vazias.

## Arquitetura e dados

Fluxo: `JogosController -> JogoService -> IJogoRepository -> JogoRepository`.

O controller recebe o HTTP e escolhe a resposta; o service converte dados e realiza consultas/regras; o repository controla a lista. `IJogoRepository` está registrado como **Singleton**; `JogoService` e `TokenService`, como **Scoped**. Um `lock` protege a lista e o incremento de IDs em requisições simultâneas; consultas devolvem cópias dos jogos.

Os dados existem somente durante a execução. Ao reiniciar a API, alterações são perdidas e os cinco exemplos voltam. Não há persistência em arquivos, banco ou Entity Framework.

```text
CP6_CatalogoJogos/
├── Controllers/
│   ├── AuthController.cs
│   └── JogosController.cs
├── DTOs/
│   ├── JogoRequest.cs
│   ├── LoginResponse.cs
│   └── RelatorioEstoqueResponse.cs
├── Models/
│   ├── Jogo.cs
│   └── LoginRequest.cs
├── OpenApi/
│   └── BearerSecurityOperationFilter.cs
├── Properties/
│   └── launchSettings.json
├── Repositories/
│   ├── IJogoRepository.cs
│   └── JogoRepository.cs
├── Services/
│   ├── JogoService.cs
│   └── TokenService.cs
├── testes/
│   └── testar_api.py
├── .gitignore
├── appsettings.json
├── CP6_CatalogoJogos.csproj
├── CP6_CatalogoJogos.http
├── Program.cs
├── README.md
└── VALIDACAO.md
```

## Testes

**Estado desta entrega:** não foi possível compilar nem executar a API no ambiente de criação, pois o SDK não está instalado e o download do SDK foi bloqueado pela rede. Não há resultado aprovado de build, testes HTTP ou autenticação pelo Scalar. Consulte `VALIDACAO.md` para os detalhes.

Com o SDK instalado, execute na pasta do projeto:

```bash
dotnet restore
dotnet build
python3 testes/testar_api.py
```

No Windows, se necessário, use `py -3 testes/testar_api.py`. Python 3.9 ou superior é necessário apenas para esse script. O comando `dotnet build` deve usar a configuração padrão Debug antes dele.

O script inicia instâncias locais próprias da API, em portas livres, e faz requisições HTTP reais. Confere login, claims, assinaturas e emissor/destinatário inválidos, proteção das sete operações, CRUD e respostas HTTP, validações, filtro inclusivo, soma do relatório e esquema de segurança OpenAPI. Confere também que o HTML do Scalar é servido.

Para a expiração, o script cria outra instância com `Jwt__ExpirationSeconds=2` apenas no processo de teste, obtém um token real, verifica seu uso antes e depois da expiração e encerra o processo. O arquivo `appsettings.json` permanece intacto.

O teste HTTP do HTML **não comprova** o funcionamento do cliente de requisições do Scalar no navegador. Faça também o procedimento manual da seção de login para validar essa parte.

O arquivo `CP6_CatalogoJogos.http` contém requisições para execução sequencial no VS Code com a extensão REST Client, ou como referência para o Insomnia/Postman. Ele captura o token e o ID criado; execute o login e o cadastro antes das operações que dependem deles.

## Critérios do professor

| Critério | Pontos | Onde está implementado | Validação funcional |
| --- | --- | --- | --- |
| JWT e Scalar | 3 | TokenService, AuthController, Program e filtro OpenAPI | Pendente |
| Camadas e DI | 2 | Controllers, Services, Repositories e Program | Pendente de build |
| CRUD protegido | 3 | JogosController e JogoRepository | Pendente |
| LINQ e relatório | 2 | JogoService | Pendente |

Os nomes e RMs dos integrantes não foram inseridos porque não foram informados para esta atividade.
