#!/bin/bash
# Скрипт для настройки Ollama для работы с Docker

echo "Настройка Ollama для работы с Docker контейнерами..."

# Создаем override директорию для systemd
sudo mkdir -p /etc/systemd/system/ollama.service.d/

# Создаем override файл
sudo tee /etc/systemd/system/ollama.service.d/override.conf > /dev/null <<EOF
[Service]
Environment="OLLAMA_HOST=0.0.0.0:11434"
EOF

# Перезагружаем systemd и перезапускаем Ollama
sudo systemctl daemon-reload
sudo systemctl restart ollama

echo "Ollama настроен. Проверяем..."
sleep 2
ss -tuln | grep 11434

echo ""
echo "Если вы видите '0.0.0.0:11434' вместо '127.0.0.1:11434', настройка успешна!"
