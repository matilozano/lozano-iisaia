import asyncio
import json
from protocol import response, ProtocolError, ZERO_UUID


class HttpServer:
    def __init__(self, dispatcher, max_body=4096, io_timeout=2, tick_ms=20):
        self.dispatcher = dispatcher
        self.max_body, self.io_timeout, self.tick_ms = max_body, io_timeout, tick_ms

    async def read_request(self, reader):
        # Read a bounded header without readline's potentially unbounded allocation.
        header = bytearray()
        while not header.endswith(b"\r\n\r\n"):
            chunk = await reader.read(1)
            if not chunk or len(header) >= 2048:
                raise ValueError("Header incompleto/excesivo")
            header.extend(chunk)
        lines = bytes(header).decode().split("\r\n")
        if lines[0] != "POST /v1/exchange HTTP/1.1":
            raise ValueError("Ruta/metodo no soportado")
        headers = {}
        for line in lines[1:]:
            if line:
                key, value = line.split(":", 1)
                key = key.strip().lower()
                if key in headers:
                    raise ValueError("Header duplicado")
                headers[key] = value.strip()
        if headers.get("content-type", "").split(";")[0].strip().lower() != "application/json" or "transfer-encoding" in headers:
            raise ValueError("Requiere JSON y Content-Length")
        length = int(headers.get("content-length", "0"))
        if not 0 < length <= self.max_body:
            raise ValueError("Body fuera de limite")
        return await reader.readexactly(length)

    async def client(self, reader, writer):
        try:
            try:
                body = await asyncio.wait_for(self.read_request(reader), self.io_timeout)
                result = self.dispatcher.handle(body)
                status = "200 OK"
            except (ValueError, UnicodeError, EOFError):
                result = response(ZERO_UUID, error=ProtocolError("INVALID_PARAMETER", "HTTP incompleto/invalido"))
                status = "400 Bad Request"
            except asyncio.TimeoutError:
                result = response(ZERO_UUID, error=ProtocolError("TIMEOUT", "Recepcion incompleta"))
                status = "408 Request Timeout"
            data = json.dumps(result).encode()
            writer.write(("HTTP/1.1 %s\r\nContent-Type: application/json\r\nContent-Length: %d\r\nConnection: close\r\n\r\n" % (status, len(data))).encode() + data)
            await asyncio.wait_for(writer.drain(), self.io_timeout)
        except (OSError, asyncio.TimeoutError):
            pass  # Output acceptance does not depend on the client reading its reply.
        finally:
            writer.close()
            try:
                await writer.wait_closed()
            except OSError:
                pass

    async def maintenance(self):
        while True:
            try:
                self.dispatcher.tick()
            except Exception as error:
                print("Controlador sin confirmacion:", type(error).__name__)
            await asyncio.sleep(self.tick_ms / 1000)

    async def run(self, host, port):
        server = await asyncio.start_server(self.client, host, port)
        print("Controlador listo", host, port)
        try:
            await self.maintenance()
        finally:
            server.close()
            await server.wait_closed()
            self.dispatcher.stop()
