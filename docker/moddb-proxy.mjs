// Runs `vsdev moddb serve` (pages from vsdev's default ./moddb) and makes it reachable from Traefik. The review server only listens on 127.0.0.1 and refuses any
// request whose Host (or a POST's Origin) is not 127.0.0.1/localhost on its own port (DNS-rebinding protection). This proxy
// listens on 0.0.0.0 and rewrites those two headers. That is safe here because the only way in is Traefik on the host's
// loopback, and its router only matches the hostname in compose.yml; do not publish this container's port.
import { spawn } from "node:child_process";
import { request, createServer } from "node:http";

const INNER = 5173;
const OUTER = Number(process.env.PORT ?? 8080);

const child = spawn("vsdev", ["moddb", "serve", "--no-open", "--port", String(INNER), ...process.argv.slice(2)], {
  stdio: "inherit",
});
child.on("exit", (code) => process.exit(code ?? 1));
for (const sig of ["SIGINT", "SIGTERM"]) process.on(sig, () => child.kill(sig));

createServer((req, res) => {
  const headers = { ...req.headers, host: `127.0.0.1:${INNER}` };
  if (headers.origin) headers.origin = `http://127.0.0.1:${INNER}`;
  const upstream = request({ host: "127.0.0.1", port: INNER, path: req.url, method: req.method, headers }, (up) => {
    res.writeHead(up.statusCode ?? 502, up.headers);
    up.pipe(res);
  });
  upstream.on("error", () => {
    res.writeHead(502, { "Content-Type": "text/plain" });
    res.end("vsdev moddb serve is not ready yet");
  });
  req.pipe(upstream);
}).listen(OUTER, "0.0.0.0", () => console.log(`proxy: 0.0.0.0:${OUTER} -> 127.0.0.1:${INNER}`));
