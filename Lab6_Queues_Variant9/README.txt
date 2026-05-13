===============================================================
  Lab6 — Асинхронная коммуникация через RabbitMQ (Вариант 9)
===============================================================

АРХИТЕКТУРА
-----------
  Publisher  →[invoices.queue]→  Processor  →[reports.queue]→  Distributor
                                     |                               |
                               [invoices.dlq]                 [reports.dlq]
                                                          [failed_messages/]

ПРЕДВАРИТЕЛЬНЫЕ ТРЕБОВАНИЯ
---------------------------
1. Установить Docker Desktop (https://www.docker.com/products/docker-desktop)

2. Запустить RabbitMQ в Docker:
   docker run -d --name rabbitmq ^
     -p 5672:5672 -p 15672:15672 ^
     -e RABBITMQ_DEFAULT_USER=guest ^
     -e RABBITMQ_DEFAULT_PASS=guest ^
     rabbitmq:3-management

3. Убедиться, что RabbitMQ запущен:
   docker ps
   или открыть Management UI: http://localhost:15672 (guest/guest)

ПОРЯДОК ЗАПУСКА
---------------
Запускать в ОТДЕЛЬНЫХ терминалах строго в порядке:

  1. Lab6_Processor   — сначала! Слушает invoices.queue
     cd Lab6_Processor && dotnet run

  2. Lab6_Distributor — второй. Слушает reports.queue + DLQ
     cd Lab6_Distributor && dotnet run

  3. Lab6_Publisher   — последний. Интерактивное меню
     cd Lab6_Publisher && dotnet run

  ИЛИ запустить все три сразу:
     run_all.bat

МЕНЮ PUBLISHER
--------------
  1. Отправить одну накладную
  2. Отправить пакет (5 штук) с прогресс-баром
  3. Отправить накладную с PDF-вложением
  4. Показать статистику отправки
  0. Выход

ЧТО ПРОИСХОДИТ
--------------
  Publisher  → создаёт MessageEnvelope<InvoiceMessage> → отправляет в invoices.queue
  Processor  → получает → валидирует → генерирует PDF (QuestPDF) → отправляет в reports.queue
  Distributor→ получает → проверяет хеш → сохраняет PDF → пишет лог

  Файлы сохраняются в:
    ./received_reports/{yyyy-MM-dd}/  — PDF-отчёты
    ./logs/distributor_{date}.log     — лог обработки
    ./failed_messages/{date}/         — архив DLQ-сообщений

ОСТАНОВКА
---------
  Ctrl+C в каждом терминале

СТРУКТУРА ПРОЕКТА
-----------------
  Lab6_Shared/    — общие модели, MessagePublisher, MessageConsumer
  Lab6_Publisher/ — издатель (интерактивное меню)
  Lab6_Processor/ — обработчик + генерация PDF
  Lab6_Distributor/ — распространитель + DLQ

NuGet пакеты
------------
  RabbitMQ.Client 6.8.1
  QuestPDF 2024.3.4 (только Processor)
===============================================================
