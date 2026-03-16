
# 🌸 Blossom Institute -- Backend API

Backend API for **Blossom Institute**, an educational management platform designed to manage courses, students, homework submissions, attendance tracking and academic performance reports.

This repository represents a **demo project** built to demonstrate backend architecture and development practices using **.NET 8**.

⚠️ **Important**  
This project contains **only test and mock data**.  
It **does NOT contain real client data** and is not connected to any production environment.

---

# 🚀 Live Demo

You can explore and test the API using the public Swagger documentation:

👉 https://blossom-demo.onrender.com/index.html

### Demo Admin Account

To test protected endpoints, click **Authorize** in Swagger and use the following credentials:

```json
{
  "email": "admin@sosa.com",
  "password": "Admin123!"
}
```

With this admin account you can:

• Create professors  
• Create students  
• Manage courses  
• Generate academic reports

---

# 🚀 Overview

Blossom Institute provides an API to manage the academic workflow of a language institute or educational center.

Main capabilities include:

• Course management  
• Student enrollment  
• Attendance tracking  
• Homework assignments and submissions  
• Teacher feedback and grading  
• Student academic reports  
• File attachments for homework and corrections

The backend follows **Clean Architecture** and **CQRS**, separating domain logic, application services, infrastructure and API layers.

---

# 🧠 Tech Stack

• .NET 8  
• ASP.NET Core Web API  
• Entity Framework Core  
• PostgreSQL  
• ASP.NET Identity  
• JWT Authentication  
• Clean Architecture  
• CQRS Pattern  
• FluentValidation  
• Swagger / OpenAPI

Optional integrations prepared in the architecture:

• Cloud storage for attachments (Cloudinary / S3 compatible)  
• Excel and PDF export for reports

---

# 🏗 Architecture

The project follows a layered architecture to keep domain logic isolated and maintainable.

BlossomInstitute

Domain  
Entities, enums and core business models

Application  
Commands, queries, validators and business use cases

Infrastructure  
Database context, EF configurations and external services

API  
Controllers, authentication and HTTP endpoints

Main patterns used:

• CQRS (Command Query Responsibility Segregation)  
• Dependency Injection  
• DTO based API contracts  
• Separation of concerns

---

# 📚 Main Features

## 🎓 Course Management

• Course creation and configuration  
• Teacher assignments  
• Student enrollment (matriculas)  
• Course based academic tracking

---

## 🧾 Attendance Tracking

Teachers can record attendance per class session.

Features include:

• Attendance status per student  
• Attendance history  
• Attendance reports per course or student

---

## 📝 Homework System

Teachers can create homework assignments for their courses.

Students can:

• Submit homework  
• Attach files or external links  
• Update submissions before correction

Teachers can:

• Provide feedback  
• Attach corrected documents  
• Assign grades

---

## ⭐ Feedback & Grading

Each homework submission can receive:

• Teacher feedback  
• Correction status  
• Numeric grade  
• Attached corrected files

The system keeps **versioned feedback**, allowing history tracking.

---

## 📊 Student Reports

The API can generate reports such as:

• Student academic summaries  
• Homework performance reports  
• Attendance reports

Export formats supported:

• Excel  
• PDF

---

# 🔐 Security

Authentication and authorization are implemented using:

• ASP.NET Identity  
• JWT Tokens  
• Role-based authorization

Supported roles:

• Admin  
• Profesor  
• Alumno

Each endpoint validates permissions according to the role.

---

# ▶ Running the Project

## 1️⃣ Clone the repository

```
git clone https://github.com/SosaUlises/blossom-institute-api
```

---

## 2️⃣ Configure the database

Update the connection string in **appsettings.json** or use **User Secrets**.

Example PostgreSQL connection:

```json
{
  "ConnectionStrings": {
    "PostgreConnectionString": "Host=localhost;Port=5432;Database=BlossomInstitute;Username=YOUR_USER;Password=YOUR_PASSWORD"
  }
}
```

---

## 3️⃣ Apply migrations

```
dotnet ef database update
```

---

## 4️⃣ Run the API

```
dotnet run
```

Swagger will be available at:

```
https://localhost:5001/swagger
```

---

# 📖 API Documentation

Swagger / OpenAPI is enabled to explore and test the API endpoints.

Main endpoint groups include:

• Authentication  
• Courses  
• Classes  
• Attendance  
• Homework  
• Homework submissions  
• Feedback  
• Reports

---

# 📌 Project Status

This repository represents a **demo and portfolio version** of the system.

It includes:

• Example data structures  
• Sample academic workflows  
• Homework submission system  
• Academic report generation

Some features are simplified to keep the focus on demonstrating backend architecture.

---

# ⚠ Disclaimer

This project was created for **demonstration and educational purposes**.

• All data included is **mock or test data**  
• No real client data is stored in this repository  
• The repository is **not a production environment**

---

# 👨‍💻 Author

**Ulises Sosa**  
Backend Developer — .NET / Clean Architecture

This project was built as part of a backend portfolio to demonstrate:

• scalable API architecture  
• clean code practices  
• domain driven design  
• modern .NET backend development
