const express = require('express');
const crypto = require('crypto');
const app = express();
const PORT = 3005;

app.use(express.json());

// Configuració d'encriptació
const algorithm = 'aes-256-cbc';
const key = crypto.scryptSync('la-meva-clau-secreta', 'salt', 32);
const iv = Buffer.alloc(16, 0); 
let storedHash = "";

console.log("--- Iniciant Servidor ---");

app.post('/encrypt', (req, res) => {
    console.log("📩 Rebut /encrypt amb:", req.body);
    const { text } = req.body;
    if (!text) return res.status(400).json({ error: "Falta el text" });

    const cipher = crypto.createCipheriv(algorithm, key, iv);
    let encrypted = cipher.update(text, 'utf8', 'hex');
    encrypted += cipher.final('hex');
    
    console.log("📤 Enviant encriptat:", encrypted);
    res.json({ encrypted });
});

app.post('/decrypt', (req, res) => {
    console.log("📩 Rebut /decrypt amb:", req.body);
    const { encrypted } = req.body;
    try {
        const decipher = crypto.createDecipheriv(algorithm, key, iv);
        let decrypted = decipher.update(encrypted, 'hex', 'utf8');
        decrypted += decipher.final('utf8');
        console.log("📤 Enviant desencriptat:", decrypted);
        res.json({ text: decrypted });
    } catch (e) {
        console.error("❌ Error en desencriptar:", e.message);
        res.status(400).json({ error: "Dades invàlides" });
    }
});

app.post('/hash', (req, res) => {
    console.log("📩 Rebut /hash");
    const { password } = req.body;
    storedHash = crypto.createHash('sha256').update(password).digest('hex');
    console.log("💾 Hash generat i guardat:", storedHash);
    res.json({ hash: storedHash });
});

app.post('/verify', (req, res) => {
    console.log("📩 Rebut /verify");
    const { password } = req.body;
    const incomingHash = crypto.createHash('sha256').update(password).digest('hex');
    const isOk = (storedHash && incomingHash === storedHash);
    console.log(`🔍 Verificació: ${isOk ? "ÈXIT" : "FALLADA"}`);
    res.json({ ok: isOk });
});

// Per evitar que la consola tanqui ràpid en errors de port
app.listen(PORT, () => {
    console.log("========================================");
    console.log(`✅ SERVIDOR FUNCIONANT A http://localhost:${PORT}`);
    console.log("Prem CTRL+C per tancar el servidor.");
    console.log("========================================");
}).on('error', (err) => {
    console.log("❌ ERROR AL SERVIDOR:", err.message);
});