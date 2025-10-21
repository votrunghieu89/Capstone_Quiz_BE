Cài SSMS cho SQL Server bản SQL Server Management Studio 21

Cài Docker

Cài RedisInsignt cho laptop

Chạy Redis bằng Docker
Config Redis with Docker: 
  Step1: Dow Docker
  Step2: open Terminal with Admin: run docker run -d --name redis -p 6379:6379 redis:8.2
  Step3: dokcer ps -> docker exec -it redis redis-cli -> PING ( nếu trả vể PONG là ok))

File tạo chay database ở FILEDB


2 Version cho SignalR là 8.0.7 cho cả Reactjs và .Net Core


Cài RabbitMQ
Step 1: Mở powerShell docker pull rabbitmq:3-management
Step 2: docker run -d --name rabbitmq `
			 -p 5672:5672 `
			 -p 15672:15672 `
			 -e RABBITMQ_DEFAULT_USER=admin `
			 -e RABBITMQ_DEFAULT_PASS=admin `
			 -v rabbitmq_data:/var/lib/rabbitmq `
			 rabbitmq:3-management
			or docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=admin -v rabbitmq_data:/var/lib/rabbitmq rabbitmq:3-management

Cài Mongo
Step 1; Cài Mongo Compass
Step 2: mở powershell docker pull mongo
Step 3: docker run -d --name capstone -p 27088:27017 -v mongo_data:/data/db -e MONGO_INITDB_ROOT_USERNAME=admin -e MONGO_INITDB_ROOT_PASSWORD=123123 mongo
Step 4: mở Mogon compass -> new connection -> mongodb://admin:123123@localhost:27088/?authSource=admin