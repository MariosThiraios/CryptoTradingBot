# Crypto Trading Bot – Deployment & Management Guide

This README provides clear, step-by-step instructions for deploying, updating, and managing your **CryptoTradingBot.Worker** on a Linux VPS (e.g., DigitalOcean Droplet) using **systemd**.

---

## 🚀 Connect to Your Server
If using an SSH key:
```bash
ssh root@134.209.146.23
```

---

## 📦 Update Your Server
```bash
apt update
apt upgrade -y
```

---

## 🛠️ Publish Your Application (Local Machine)
Inside your project folder:
```bash
cd CryptoTradingBot.Worker

dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
```

---

## 📤 Upload Published Files to the Server (SCP)
Navigate to the `publish` folder:
```bash
cd publish
```

Upload the files:
```bash
scp -r * root@134.209.146.23:/root/trading-bot/
```

---

## ⚙️ Create the systemd Service
On the server:
```bash
nano /etc/systemd/system/trading-bot.service
```

Paste the following:
```ini
[Unit]
Description=Crypto Trading Bot Worker Service
After=network.target

[Service]
Type=simple
WorkingDirectory=/root/trading-bot
ExecStart=/usr/bin/dotnet /root/trading-bot/CryptoTradingBot.Worker.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=trading-bot
User=root
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
```

---

## 🔄 Load & Start the Service
```bash
systemctl daemon-reload
systemctl enable trading-bot.service
systemctl start trading-bot.service
systemctl status trading-bot.service
```

---

## 📜 Viewing Logs
Live logs:
```bash
journalctl -u trading-bot.service -f
```

---

## 🧰 Managing the Bot
Stop the service:
```bash
systemctl stop trading-bot.service
```

Start the service:
```bash
systemctl start trading-bot.service
```

Restart the service:
```bash
systemctl restart trading-bot.service
```

View service status:
```bash
systemctl status trading-bot.service
```

Disable automatic start on boot:
```bash
systemctl disable trading-bot.service
```

---

## 🔄 Updating Your Bot (Future Deployments)
### On your local machine:
```bash
cd CryptoTradingBot.Worker

dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
cd publish
scp -r * root@134.209.146.23:/root/trading-bot/
```

### On the server:
```bash
systemctl restart trading-bot.service
```

---

## ✅ Your Crypto Trading Bot Is Now Ready!
This guide should make deployments, updates, and maintenance quick and clean. Feel free to copy this README directly into your repository.

