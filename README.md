# CP6 - Catálogo de Jogos

API feita em C# com ASP.NET Core (.NET 8) para o Checkpoint 6 de .NET. O projeto permite cadastrar, consultar, atualizar e excluir jogos. Também tem login com JWT, filtro de jogos e um relatório de estoque.

## Tecnologias

- .NET 8 e ASP.NET Core Web API
- JWT Bearer para autenticação
- Scalar para testar as rotas
- Swashbuckle para gerar o OpenAPI
- LINQ para filtros e relatórios
- Lista em memória para guardar os jogos

Não é necessário configurar banco de dados.

## Como executar

Com o SDK do .NET 8 instalado, abra o terminal na pasta do projeto e execute:

```bash
dotnet restore
dotnet build
dotnet run --launch-profile http
```

A documentação fica em: **http://localhost:5080/scalar/v1**

## Login

No Scalar, abra `POST /api/auth/login` e envie:

```json
{
  "username": "Mauricio",
  "password": "123456"
}
```

Copie o `token` retornado e coloque na autenticação **Bearer** do Scalar. Depois disso, será possível testar as rotas de jogos. O login é público, mas as rotas de jogos exigem token.

As credenciais e a chave JWT são apenas para testes do projeto.

## Endpoints

| Método | Rota | O que faz |
| --- | --- | --- |
| POST | `/api/auth/login` | Faz login e retorna o token |
| GET | `/api/jogos` | Lista os jogos |
| GET | `/api/jogos/{id}` | Busca um jogo pelo ID |
| POST | `/api/jogos` | Cadastra um jogo |
| PUT | `/api/jogos/{id}` | Atualiza um jogo |
| DELETE | `/api/jogos/{id}` | Exclui um jogo |
| GET | `/api/jogos/busca?plataforma=PC&precoMaximo=100` | Filtra por plataforma e preço |
| GET | `/api/jogos/relatorio-estoque` | Mostra o estoque agrupado por plataforma |

As rotas retornam os códigos HTTP adequados, como `200`, `201`, `204`, `400`, `401` e `404`, conforme a operação.

### Exemplo de cadastro

```json
{
  "titulo": "Minecraft",
  "plataforma": "PC",
  "genero": "Sobrevivência",
  "preco": 99.90,
  "anoLancamento": 2011,
  "estoque": 15
}
```

O ID é gerado automaticamente. Para atualizar um jogo, utilize o mesmo formato no `PUT`, informando o ID na URL.

## Organização do código

- **Controllers:** recebem as requisições e retornam as respostas HTTP.
- **Services:** concentram as regras de negócio, o filtro e o relatório.
- **Repositories:** cuidam da lista de jogos em memória.
- **Models e DTOs:** definem os dados usados pela API.
- **Program.cs:** configura as dependências, o JWT e a documentação.

O repositório é registrado como Singleton. Os dados ficam salvos enquanto a API estiver em execução. Quando ela é reiniciada, a lista volta aos cinco jogos iniciais.

O filtro utiliza LINQ para buscar jogos de uma plataforma com preço até o limite informado. O relatório agrupa por plataforma e calcula o valor total do estoque com `preco × estoque`.

## Testes realizados

O projeto foi compilado no Windows com `dotnet build`:

```text
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
```

Também foram feitos testes manuais no Scalar com os seguintes resultados:

| Teste | Resultado |
| --- | --- |
| Login com usuário e senha corretos | 200 OK |
| Login com senha incorreta | 401 Unauthorized |
| Listagem de jogos com token | 200 OK |
| Cadastro de jogo | 201 Created |
| Busca de jogo por ID | 200 OK |
| Atualização de jogo | 200 OK |
| Exclusão de jogo | 204 No Content |
| Filtro por plataforma e preço | Resultado esperado |
| Relatório de estoque | 200 OK, agrupamento e valores conferidos |
| Acesso sem token | 401 Unauthorized |
| Acesso com token inválido | 401 Unauthorized |
| Busca por ID inexistente | 404 Not Found |

Esses resultados são dos testes manuais realizados no Scalar. O script `testes/testar_api.py` também está disponível, mas não foi executado nesta validação.
