# If using SSH key:
ssh root@YOUR_DROPLET_IP

# Update package lists
apt update

# Upgrade installed packages
apt upgrade -y

cd CryptoTradingBot.Worker

# Publish the application for Linux
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish

- Upload Your Application to the Server
Navigate to the publish folder
cd publish

Upload files to the server using SCP
scp -r * root@YOUR_DROPLET_IP:/root/trading-bot/

# Create the service file
nano /etc/systemd/system/trading-bot.service

[Unit]
Description=Crypto Trading Bot Worker Service
After=network.target

[Service]
Type=simple / notify
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



# Reload systemd to recognize the new service
systemctl daemon-reload

# Enable the service to start on boot
systemctl enable trading-bot.service

# Start the service now
systemctl start trading-bot.service

# Check the status
systemctl status trading-bot.service



# View live logs
journalctl -u trading-bot.service -f

# View last 100 lines
journalctl -u trading-bot.service -n 100

# View logs since today
journalctl -u trading-bot.service --since today



# View log files
cd /root/trading-bot/logs
ls -la
cat trading-bot-*.log



# Stop the bot
systemctl stop trading-bot.service

# Start the bot
systemctl start trading-bot.service

# Restart the bot
systemctl restart trading-bot.service

# View status
systemctl status trading-bot.service

# View logs in real-time
journalctl -u trading-bot.service -f

# Disable auto-start on boot
systemctl disable trading-bot.service


🔄 Updating Your Bot (Future Deployments)
When you make changes and want to update:

On local machine:

bash   cd CryptoTradingBot.Worker
   dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
   cd publish
   scp -r * root@YOUR_DROPLET_IP:/root/trading-bot/

On server:

bash   systemctl restart trading-bot.service
