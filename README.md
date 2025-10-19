Product Management WPF App 

This is a WPF application built as a school project using C# 12 and .NET 9.  
The application follows MVVM and a layered architecture (Domain, Application, Infrastructure, Presentation).  

It allows you to manage products with categories and manufacturers,  
with features for creating, editing, deleting, validating, and saving data to JSON.  
Unit tests are written with xUnit for all core functionality (except ReadAsync, as instructed).  

Features  
- Navigate between product list, product create and edit views 
- Create, update, delete and list products 
- Products include Category and Manufacturer (separate entities) 
- Prevents duplicate product names and empty products 
- Data is stored in JSON files 
- Uses Service Pattern with interfaces and repository abstraction to separate logic from data access 
- Unit tests with xUnit  

Project Structure  
- Domain – Entities, interfaces, results and extensions 
- ApplicationLayer – Services, DTOs, validation, factories 
- Infrastructure – JSON repository implementation 
- Presentation – WPF views (MVVM) and navigation 
- Tests – Unit tests with mocks for services, and repository tests with temporary files.  

Requirements Fulfilled  
- Two views with navigation 
- CRUD operations for products 
- Validation and duplicate checks 
- JSON persistence with load and save 
- Unit tests for all functionality (except ReadAsync, as instructed) 
- Git branching workflow 
