"""Testes HTTP reais. Requer dotnet build e Python 3.9+, sem bibliotecas externas.

Inicia sua própria API em porta local livre, com configuração temporária de JWT.
Não acessa nem altera a instância usada manualmente pelo aluno.
"""

import base64
import hashlib
import hmac
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import subprocess
import sys
import tempfile
import time
from decimal import Decimal
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, urlopen

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / "bin/Debug/net8.0/CP6_CatalogoJogos.dll"
KEY = secrets.token_hex(32)
ISSUER = "CP6.Testes"
AUDIENCE = "CP6.Testes.Cliente"
LOGIN = {"username": "Mauricio", "password": "123456"}
BASE = ""
TOTAL = 0


def request(method, path, data=None, token=None):
    headers = {}
    if token is not None:
        headers["Authorization"] = "Bearer " + token
    body = None
    if data is not None:
        headers["Content-Type"] = "application/json"
        body = json.dumps(data).encode()
    req = Request(BASE + path, data=body, headers=headers, method=method)
    try:
        response = urlopen(req, timeout=8)
    except HTTPError as error:
        response = error
    with response:
        raw = response.read().decode()
        try:
            content = json.loads(raw, parse_float=Decimal) if raw else None
        except json.JSONDecodeError:
            content = raw
        return response.status, content, response.headers


def check(description, condition):
    global TOTAL
    if not condition:
        raise AssertionError(description)
    TOTAL += 1
    print("OK - " + description, flush=True)


def b64(data):
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode()


def assinar(claims, key=KEY):
    header = b64(b'{"alg":"HS256","typ":"JWT"}')
    payload = b64(json.dumps(claims).encode())
    message = (header + "." + payload).encode()
    return message.decode() + "." + b64(hmac.new(key.encode(), message, hashlib.sha256).digest())


class ApiTemporaria:
    def __init__(self, expiration_seconds=3600):
        self.expiration_seconds = expiration_seconds
        self.process = None
        self.log = None

    def __enter__(self):
        global BASE
        with socket.socket() as sock:
            sock.bind(("127.0.0.1", 0))
            port = sock.getsockname()[1]
        BASE = f"http://127.0.0.1:{port}"
        env = os.environ.copy()
        env.update({
            "ASPNETCORE_ENVIRONMENT": "Development",
            "ASPNETCORE_URLS": BASE,
            "Jwt__Key": KEY,
            "Jwt__Issuer": ISSUER,
            "Jwt__Audience": AUDIENCE,
            "Jwt__ExpirationSeconds": str(self.expiration_seconds),
        })
        self.log = tempfile.TemporaryFile(mode="w+")
        self.process = subprocess.Popen(
            ["dotnet", str(DLL)], cwd=ROOT, env=env,
            stdout=self.log, stderr=subprocess.STDOUT,
        )
        for _ in range(100):
            if self.process.poll() is not None:
                break
            try:
                if request("GET", "/openapi/v1.json")[0] == 200:
                    return self
            except (URLError, OSError):
                pass
            time.sleep(0.1)
        self.log.seek(0)
        output = self.log.read()
        self.__exit__(None, None, None)
        raise RuntimeError("A API não iniciou.\n" + output)

    def __exit__(self, *_):
        if self.process is not None:
            if self.process.poll() is None:
                self.process.terminate()
                try:
                    self.process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    self.process.kill()
                    self.process.wait()
        if self.log is not None:
            self.log.close()


def testar():
    with ApiTemporaria():
        status, auth, _ = request("POST", "/api/auth/login", LOGIN)
        check("Login válido retorna 200 e JWT", status == 200 and len(auth["token"].split(".")) == 3)
        token = auth["token"]
        encoded = token.split(".")[1]
        claims = json.loads(base64.urlsafe_b64decode(encoded + "=" * (-len(encoded) % 4)))
        check("JWT tem sub, jti, issuer, audience e expiração", claims["sub"] == "Mauricio"
              and bool(claims["jti"]) and claims["iss"] == ISSUER
              and claims["aud"] == AUDIENCE and claims["exp"] > time.time())
        _, outro, _ = request("POST", "/api/auth/login", LOGIN)
        payload_outro = outro["token"].split(".")[1]
        claims_outro = json.loads(base64.urlsafe_b64decode(payload_outro + "=" * (-len(payload_outro) % 4)))
        check("Tokens diferentes têm jti diferente", claims_outro["jti"] != claims["jti"])
        check("Senha incorreta retorna 401", request("POST", "/api/auth/login", {**LOGIN, "password": "errada"})[0] == 401)
        check("Login vazio retorna 400", request("POST", "/api/auth/login", {})[0] == 400)

        jogo = {"titulo": "Jogo de teste CP6", "plataforma": "PC", "genero": "RPG",
                "preco": 50, "anoLancamento": 2024, "estoque": 3}
        protected = [
            ("GET", "/api/jogos", None),
            ("GET", "/api/jogos/1", None),
            ("POST", "/api/jogos", jogo),
            ("PUT", "/api/jogos/1", jogo),
            ("DELETE", "/api/jogos/1", None),
            ("GET", "/api/jogos/busca?plataforma=PC&precoMaximo=300", None),
            ("GET", "/api/jogos/relatorio-estoque", None),
        ]
        for method, path, data in protected:
            check(f"Sem token: {method} {path} retorna 401", request(method, path, data)[0] == 401)
        check("Token malformado retorna 401", request("GET", "/api/jogos", token="invalido")[0] == 401)
        check("Assinatura inválida retorna 401", request("GET", "/api/jogos", token=assinar(claims, "outra-chave-publica-de-teste-1234567890"))[0] == 401)
        for field in ("iss", "aud"):
            adulterado = assinar({**claims, field: "outro-valor"})
            check(f"Claim {field} incorreta retorna 401", request("GET", "/api/jogos", token=adulterado)[0] == 401)

        status, iniciais, _ = request("GET", "/api/jogos", token=token)
        check("Listagem autenticada retorna cinco exemplos", status == 200 and len(iniciais) == 5)
        check("Exemplos usam cinco plataformas diferentes", len({j["plataforma"] for j in iniciais}) == 5)
        status, criado, headers = request("POST", "/api/jogos", jogo, token)
        novo_id = criado["id"]
        check("Cadastro retorna 201, objeto e Location", status == 201 and novo_id > 5
              and criado["titulo"] == jogo["titulo"] and headers.get("Location", "").endswith(f"/api/jogos/{novo_id}"))
        status, encontrado, _ = request("GET", f"/api/jogos/{novo_id}", token=token)
        check("Busca por ID retorna o objeto criado", status == 200 and encontrado == criado)
        for method, data in (("GET", None), ("PUT", jogo), ("DELETE", None)):
            check(f"{method} de ID inexistente retorna 404", request(method, "/api/jogos/2147483647", data, token)[0] == 404)
        alteracao = {**jogo, "titulo": "Jogo atualizado", "preco": 75, "estoque": 2}
        status, atualizado, _ = request("PUT", f"/api/jogos/{novo_id}", alteracao, token)
        check("Atualização retorna 200 e preserva ID", status == 200 and atualizado["id"] == novo_id
              and atualizado["preco"] == 75 and atualizado["estoque"] == 2)
        check("Atualização permanece na listagem", request("GET", f"/api/jogos/{novo_id}", token=token)[1] == atualizado)

        for campo, valor in (("titulo", "   "), ("plataforma", ""), ("genero", ""),
                             ("preco", -1), ("estoque", -1), ("anoLancamento", 0), ("anoLancamento", 9999)):
            invalido = {**jogo, campo: valor}
            check(f"Cadastro inválido ({campo}={valor!r}) retorna 400", request("POST", "/api/jogos", invalido, token)[0] == 400)
        check("PUT inválido retorna 400", request("PUT", f"/api/jogos/{novo_id}", {**jogo, "estoque": -1}, token)[0] == 400)
        check("PUT inválido preserva dados anteriores", request("GET", f"/api/jogos/{novo_id}", token=token)[1] == atualizado)

        params = urlencode({"plataforma": "pc", "precoMaximo": 75})
        status, filtrados, _ = request("GET", "/api/jogos/busca?" + params, token=token)
        _, todos, _ = request("GET", "/api/jogos", token=token)
        esperados = [j for j in todos if j["plataforma"].lower() == "pc" and j["preco"] <= 75]
        check("Filtro aplica plataforma sem case e limite inclusivo de preço", status == 200 and filtrados == esperados and atualizado in filtrados)
        check("Filtro sem resultados retorna lista vazia", request("GET", "/api/jogos/busca?plataforma=Inexistente&precoMaximo=0", token=token)[:2] == (200, []))
        for query in ("", "?plataforma=PC", "?precoMaximo=10", "?plataforma=PC&precoMaximo=-1", "?plataforma=PC&precoMaximo=abc"):
            check(f"Filtro inválido ({query}) retorna 400", request("GET", "/api/jogos/busca" + query, token=token)[0] == 400)

        status, relatorio, _ = request("GET", "/api/jogos/relatorio-estoque", token=token)
        agregado = {}
        for j in todos:
            chave = j["plataforma"].lower()
            titulos, total = agregado.get(chave, (0, Decimal(0)))
            agregado[chave] = titulos + 1, total + Decimal(str(j["preco"])) * j["estoque"]
        recebido = {r["plataforma"].lower(): (r["quantidadeTitulos"], Decimal(str(r["valorTotalEstoque"]))) for r in relatorio}
        check("Relatório agrupa e calcula Count e Sum(Preço * Estoque)", status == 200 and recebido == agregado)
        status, body, _ = request("DELETE", f"/api/jogos/{novo_id}", token=token)
        check("Exclusão retorna 204 sem corpo", status == 204 and body is None)
        check("Jogo excluído retorna 404", request("GET", f"/api/jogos/{novo_id}", token=token)[0] == 404)
        status, novo, _ = request("POST", "/api/jogos", jogo, token)
        check("Identificador incremental não reutiliza ID excluído", status == 201 and novo["id"] > novo_id)
        request("DELETE", f"/api/jogos/{novo['id']}", token=token)

        status, spec, _ = request("GET", "/openapi/v1.json")
        check("OpenAPI disponível", status == 200 and spec["openapi"].startswith("3."))
        esquema = spec["components"]["securitySchemes"]["Bearer"]
        check("OpenAPI define HTTP Bearer JWT", esquema["type"] == "http" and esquema["scheme"] == "bearer" and esquema["bearerFormat"] == "JWT")
        login_spec = spec["paths"]["/api/auth/login"]["post"]
        check("OpenAPI mantém login público", not login_spec.get("security") and not spec.get("security"))
        jogos_operations = [op for path, ops in spec["paths"].items() if path.startswith("/api/jogos")
                            for method, op in ops.items() if method in {"get", "post", "put", "delete"}]
        check("OpenAPI exige Bearer nas sete operações de jogos", len(jogos_operations) == 7
              and all(any("Bearer" in item for item in op.get("security", [])) for op in jogos_operations))
        status, html, _ = request("GET", "/scalar/v1")
        check("Página HTML do Scalar disponível", status == 200 and isinstance(html, str) and "scalar" in html.lower())

    # Outra instância com vida curta; appsettings.json permanece intacto.
    with ApiTemporaria(expiration_seconds=2):
        _, auth, _ = request("POST", "/api/auth/login", LOGIN)
        token = auth["token"]
        check("Token curto é válido antes de expirar", request("GET", "/api/jogos", token=token)[0] == 200)
        time.sleep(3)
        check("Mesmo token expirado retorna 401", request("GET", "/api/jogos", token=token)[0] == 401)
    print(f"\n{TOTAL} verificações HTTP passaram.")
    print("Pendente de verificação manual: executar login e requisição autenticada dentro do Scalar.")


if __name__ == "__main__":
    if shutil.which("dotnet") is None:
        print("NÃO EXECUTADO: instale o SDK do .NET 8 e execute dotnet build primeiro.", file=sys.stderr)
        sys.exit(2)
    if not DLL.is_file():
        print("NÃO EXECUTADO: execute dotnet build na raiz do projeto primeiro.", file=sys.stderr)
        sys.exit(2)
    try:
        testar()
    except (AssertionError, OSError, URLError, KeyError, RuntimeError) as error:
        print(f"FALHOU: {error}", file=sys.stderr)
        sys.exit(1)
