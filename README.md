```markdown
# Insurance Claim Management Application

## Overview
The Insurance Claim Management application is a web-based solution
designed to streamline the process of managing insurance claims and user data.
It allows users to submit, update, and view their claims while also managing user information efficiently.

## Purpose
The purpose of this application is to provide a user-friendly interface for managing insurance claims,
making it easier for users and administrators to track and update claim statuses.
It aims to enhance the overall efficiency of insurance claim processing.

## Features
- **User Management**: 
  - Create, read, update, and delete user profiles.
  - Store user details including name, email, username, and password.

- **Claims Management**: 
  - Create, read, update, and delete insurance claims.
  - Track claim types, descriptions, amounts, statuses, and associated user information.
  - Format amounts to display with one decimal place.

- **Dashboard**: 
  - View lists of users and claims in a comprehensive dashboard.

  ## Screenshots

### Claims Page
![Claims Page](screenshots/ClaimsPage.jpg)

### Dashboard
![Dashboard](screenshots/Dashboard.jpg)

### Delete Claim
![Delete Claim](screenshots/DeleteClaim.jpg)

### Delete User
![Delete User](screenshots/DeleteUser.jpg)

### Edit Claim
![Edit Claim](screenshots/EditClaim.jpg)

### Edit User
![Edit User](screenshots/EditUser.jpg)

### Users Page
![Users Page](screenshots/UsersPage.jpg)

## Tech Stack
- **Frontend**: 
  - ASP.NET Core Razor Pages for building dynamic web pages.
  - HTML, CSS, and Bootstrap for responsive design and layout.

- **Backend**: 
  - ASP.NET Core for building the server-side logic and handling requests.
  - Entity Framework Core for data access and manipulation.

- **Database**: 
  - MySQL for storing user and claim data.

- **Development Tools**: 
  - Visual Studio for development.
  - Git for version control.

## Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/InsuranceClaimManagement.git
   ```
2. Navigate to the project directory:
   ```bash
   cd InsuranceClaimManagement
   ```
3. Restore the dependencies:
   ```bash
   dotnet restore
   ```
4. Update the database connection string in `appsettings.json`.
5. Run the migrations to set up the database:
   ```bash
   dotnet ef database update
   ```
6. Start the application:
   ```bash
   dotnet run
   ```

## Usage
- Access the application through your web browser at `http://localhost:5000`.
- Register a new user or log in to manage claims.
- Use the dashboard to view and manage users and claims.

## Contributing
Contributions are welcome! Please follow these steps to contribute:
1. Fork the repository.
2. Create a new branch for your feature or bug fix.
3. Make your changes and commit them.
4. Push your branch to your forked repository.
5. Create a pull request describing your changes.

Feel free to adjust any links or specific information to match your project details!
