FROM ubuntu:22.04

# Установка переменных окружения для избежания интерактивных запросов
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=UTC

# Обновление пакетов и установка необходимых компонентов
RUN apt-get update && apt-get install -y \
    pandoc \
    texlive-xetex \
    texlive-latex-base \
    texlive-latex-recommended \
    texlive-fonts-recommended \
    fonts-liberation \
    fonts-dejavu-core \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Создание рабочей директории
WORKDIR /workspace

# Команда для поддержания контейнера в рабочем состоянии
CMD ["tail", "-f", "/dev/null"]
