// Local disposable acceptance stub. Does not log request content or headers.
const http = require('http');
http.createServer((req, res) => {
  let bytes = 0;
  req.on('data', chunk => { bytes += chunk.length; if (bytes > 100000) req.destroy(); });
  req.on('end', () => {
    setTimeout(() => {
      res.writeHead(Number(process.env.STATUS || 503), { 'Content-Type': 'application/json', 'Retry-After': '1' });
      res.end(JSON.stringify({ error: { message: 'Local acceptance provider unavailable' } }));
    }, Number(process.env.DELAY_MS || 5000));
  });
}).listen(8080, '0.0.0.0');
