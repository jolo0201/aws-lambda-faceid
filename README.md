# aws-lambda-faceid
A sample serverless app that connects and get date from the face recognition devices to a serverless database.

### Architectural Diagram
![](AWSLambdaFaceRecog/img/architecute_diagram.jpg)

### Case
To connect face recognition devices to cloud using a serverless approach.

### Goal
To eliminate hardware usage (PC or Server) and limit the usage cost.

### Pre-requisite
Devices must be configure via port forwarding to access them to the cloud.

### AWS Services used
- AWS Lambda
- AWS Aurora (RDS) MySQL 5.7
- AWS Eventbridge (Cloudwatch)

### Function
It is developed using a .Net Core C# AWS Lambda Project.
C# was the best option because to communicate among the list of devices, it needed the SDK (DLL) of Face ID to connect and execute the commands.

### How it runs?
It uses cron job (Eventbridge) trigger with a fixed time of 5 mins to invoke the function (the function can be invoked using an API Gateway, link or a trigger).
When invoked, the function checks the list of active devices, connect to them and try to raid data from the device and sync it to Aurora DB.

### How to invoke
The function can be manually invoke using this link: [Invoke Me](1BvTfDQa4VVtLBkknXYZHy67oE61SMpZNe.lambda-url.ap-southeast-1.on.aws)

### Copyright 2023
