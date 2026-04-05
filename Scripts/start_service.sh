#!/bin/bash

cd /var/www/html/

# Required for Syncfusion DocIORenderer (Word-to-PDF) / SkiaSharp on Linux - avoids LibSkiaSharp not found and 502/5xx
# libuuid1 provides uuid_generate_random required by libSkiaSharp.so (symbol lookup error without it)
sudo apt-get update -y
sudo apt-get install -y libfontconfig1 libfreetype6 libuuid1

sudo sed -i 's/http:\/\/localhost:8080\//http:\/\/localhost:5004\//g' /etc/apache2/sites-available/000-default.conf
sudo cp scripts/recruitcrm_syncfusion.service /etc/systemd/system/recruitcrm_syncfusion.service
sudo a2enmod proxy proxy_http

wget -qO - https://artifacts.elastic.co/GPG-KEY-elasticsearch | sudo gpg --dearmor -o /usr/share/keyrings/elastic-keyring.gpg
sudo apt-get install apt-transport-https -y
echo "deb [signed-by=/usr/share/keyrings/elastic-keyring.gpg] https://artifacts.elastic.co/packages/9.x/apt stable main" | sudo tee -a /etc/apt/sources.list.d/elastic-9.x.list
sudo apt-get update -y && sudo apt-get install logstash -y

curl -L -O https://artifacts.elastic.co/downloads/beats/filebeat/filebeat-8.14.3-arm64.deb
sudo dpkg -i filebeat-8.14.3-arm64.deb

if sudo grep -R "ENABLE_LOGGING=1" /var/www/html/.env
then
    sudo service filebeat status > /dev/null || sudo service filebeat start
    sudo systemctl status logstash > /dev/null || sudo systemctl start logstash
else
    sudo service filebeat status > /dev/null && sudo service filebeat stop
    sudo systemctl status logstash > /dev/null && sudo systemctl stop logstash
fi
if sudo grep -R "CUBEAPM_ENABLE=1" /var/www/html/templates/processed/.env
then
    sudo service otelcol-contrib restart
else 
    sudo service otelcol-contrib stop
fi

# Restore and build with explicit Linux RID so correct SkiaSharp/HarfBuzz native assets are used (fixes 502 / LibSkiaSharp on Linux)
# Do not clear NuGet cache here; it can cause NETSDK1064 and flaky restores. If you upgraded Syncfusion, clear cache once manually then redeploy.
ARCH=$(uname -m)
if [ "$ARCH" = "x86_64" ]; then RID=linux-x64; elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then RID=linux-arm64; else RID=linux-x64; fi
sudo dotnet restore --force -r "$RID"
sudo dotnet clean
sudo dotnet build -r "$RID" -c Release --no-incremental

sudo systemctl daemon-reload
sudo systemctl enable recruitcrm_syncfusion
sudo systemctl restart recruitcrm_syncfusion
sudo systemctl restart apache2
sudo systemctl status recruitcrm_syncfusion
sudo systemctl status apache2

