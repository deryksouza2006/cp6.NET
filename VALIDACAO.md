# Registro de validação da entrega

## Resultado real

| Verificação | Resultado |
| --- | --- |
| Leitura integral do enunciado PDF | Concluída, por extração de texto de todas as páginas |
| Correspondência dos arquivos com o escopo solicitado | Revisão estática realizada |
| `appsettings.json` e `launchSettings.json` | JSON válido |
| Arquivo `.csproj` | XML válido, alvo `net8.0` confirmado |
| `dotnet restore` | Não executou: `dotnet: command not found` |
| `dotnet build` | Não executou: `dotnet: command not found` |
| Instalação do SDK | Download nos endereços oficiais não pôde ser concluído: timeout/bloqueio de rede |
| Sintaxe do script Python | Verificada com `python3 -m py_compile testes/testar_api.py` |
| Execução do script de testes | Interrompida antes dos testes: SDK ausente (código de saída 2) |
| Testes HTTP da API | Pendentes |
| Página e requisições na interface Scalar | Pendentes |

Nenhum resultado de compilação, execução HTTP ou uso da interface é apresentado como aprovado. O código e os roteiros estão preparados para validação em uma máquina com SDK do .NET 8.

## Roteiro para concluir

1. Extraia o projeto e abra a pasta que contém o `.csproj`.
2. Execute `dotnet restore` e `dotnet build`.
3. Execute `python3 testes/testar_api.py` (ou `py -3` no Windows). O script para na primeira falha e encerra os processos que iniciou.
4. Execute `dotnet run --launch-profile http`.
5. Abra `http://localhost:5080/scalar/v1`, faça login com `Mauricio` / `123456` e copie o token.
6. Configure Bearer no cliente do Scalar e execute as operações protegidas, conferindo também o 401 ao remover a autenticação.
7. Registre os resultados reais antes da entrega ao professor.

O script verifica o documento OpenAPI e a resposta HTML do Scalar por HTTP. A execução de requisições com Bearer dentro da interface exige a verificação manual do navegador.
