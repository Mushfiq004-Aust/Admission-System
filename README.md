# School Admission System

An online admission system for a private school. It follows a **first-come, first-served** process, meaning students get seats based on who applies first.

Parents can enter a student's 17-digit birth registration number, choose a class and shift, and temporarily reserve a seat. They can then confirm the admission through a simulated payment.

School staff have a separate admin portal where they can create classes, check applicants, and monitor admissions.

The system automatically releases seats when a reservation expires or is cancelled.

## 1. Technologies Used

| Part | Technology |
|---|---|
| Backend | C# and ASP.NET Core (.NET 8) |
| Frontend | React and TypeScript |
| Database | PostgreSQL |
| Testing | xUnit and Vitest |
| Running the project | Docker and Docker Compose |

## 2. How to Run the Project

You only need **Docker Desktop** to run the complete project.

### Step 1: Install Docker

Download and install Docker Desktop:

https://www.docker.com/products/docker-desktop/

Open Docker Desktop and wait until it shows that Docker is running.

### Step 2: Open the project folder

Open a terminal inside the folder containing `docker-compose.yml`.

### Step 3: Start the project

Run:

```bash
docker compose up --build
```

Wait for the services to start. The first run may take a few minutes because Docker needs to download and build the required files.

### Step 4: Open the website

Once everything is running, open:

- **Student Portal:** http://localhost:3000
- **Admin Portal:** http://localhost:3000/admin
- **Backend API:** http://localhost:8080
- **Health Check:** http://localhost:8080/health

You do not need to install .NET, Node.js, or PostgreSQL separately when using Docker.

### Step 5: Stop the project

Press `Ctrl + C` in the terminal, then run:

```bash
docker compose down
```

Your classes and applications will remain saved.

To start the project again, simply run:

```bash
docker compose up
```

**Important:** Do not use `docker compose down -v` unless you want to delete the saved database data.

## 3. How to Use the Student Portal

Open http://localhost:3000.

Parents and students do not need a password to use this portal.

### Step 1: Enter the birth registration number

Enter the student's 17-digit birth registration number and click **Use this number**.

The number is saved in the browser, so you do not need to enter it every time.

### Step 2: Browse available classes

You can:
- View available classes and shifts.
- Search for a class.
- Check total seats, reserved seats, confirmed seats, and available seats.

### Step 3: Apply for a class

Click **Apply** on your preferred class.

If a seat is available, the system temporarily reserves it for you.

You will see a countdown showing how much time is left to confirm your admission.

### Step 4: Confirm your admission

Click **Pay Admission Fee** to complete the simulated payment.

Your application status will change to `CONFIRMED`.

You can also cancel your application or let the timer expire. In either case, the reserved seat becomes available again.

### Step 5: Check your applications

Open **My Applications** to see your previous applications and their current statuses.

Your applications remain saved even after refreshing or reopening the browser.

## 4. How to Use the Admin Portal

Open http://localhost:3000/admin.

The admin portal is separate from the student portal and requires a login.

**Default demo credentials:**

- Username: `admin`
- Password: `admin123`

### Admin Dashboard

The dashboard has four main sections:

**Overview**

Shows a summary of classes, seats, and recent applications.

**Class List**

Shows all created classes.

- Search for a class.
- Browse 20 classes at a time.
- Click a class to see its applicants.
- Check which students have confirmed their admission and which students have temporary reservations.

**Application Log**

Shows applications from all classes.

You can search by student registration number, class, or application ID.

You can also filter applications by status:

- `HELD` – Seat temporarily reserved.
- `CONFIRMED` – Admission completed.
- `EXPIRED` – Reservation time ran out.
- `CANCELLED` – Application cancelled.

**Create Class**

Allows the admin to create a new class by entering its name and total number of seats.

Each shift should be created separately. For example:

- Class 6 - Morning Shift
- Class 6 - Day Shift

Each class has its own seat capacity.

The applicant list automatically refreshes every five seconds.

### Admin Login Settings

The default credentials are for demonstration purposes.

You can change them using environment variables in a `.env` file:

| Variable | Purpose |
|---|---|
| `ADMIN_USERNAME` | Admin username |
| `ADMIN_PASSWORD` | Admin password |
| `JWT_SECRET` | Secret used for admin login security |
| `JWT_EXPIRY_MINUTES` | Login session duration |

The `.env.example` file contains the available settings.

## 5. How the Admission Process Works

The system uses four application statuses:

| Status | Meaning |
|---|---|
| `HELD` | A seat is temporarily reserved. |
| `CONFIRMED` | The admission has been completed. |
| `EXPIRED` | The reservation time has ended. |
| `CANCELLED` | The application was cancelled. |

By default, a reservation lasts **10 minutes**.

The reservation duration can be changed using `HOLD_DURATION_SECONDS`.

For example, setting it to `30` gives students 30 seconds to confirm their admission.

### How does the system prevent overselling?

Imagine a class has only 10 seats, but 100 students try to apply at the same time.

The system checks seat availability directly in the database before creating each reservation.

Only 10 students can successfully reserve seats. The remaining requests are rejected because the class is full.

This works even when multiple backend servers are running.

## 6. Automated Tests

The project includes automated tests to check whether the system works correctly.

### Backend Tests

The backend tests check things such as:

- Creating and viewing classes.
- Applying for a class.
- Rejecting applications when a class is full.
- Preventing the same student from making duplicate active applications.
- Confirming an application.
- Cancelling an application.
- Automatically expiring reservations.
- Preventing students from accessing other students' applications.
- Admin login and access restrictions.
- Handling multiple students applying at the same time.

The tests use a real PostgreSQL database to check the admission and concurrency rules.

### Frontend Tests

The frontend tests check:

- Student registration number validation.
- Saving and restoring student information.
- API requests and error handling.
- Admin login.
- Protecting admin pages from unauthorised access.
- Keeping student and admin portals separate.

### Running the Tests with Docker

Backend:

```bash
docker compose -f docker-compose.test.yml run --rm tests
```

Frontend:

```bash
docker compose -f docker-compose.test.yml run --rm frontend-tests
```

### Additional Concurrency Test

The project also includes an optional test that runs two backend servers connected to the same database.

Run:

```bash
docker compose --profile bonus up --build
```

Then, in another terminal:

```bash
node loadtest/multi-instance-test.mjs
```

The test sends 200 simultaneous applications for a class with 10 seats.

The expected result is:

- 10 successful applications.
- 190 rejected applications.
- No more than 10 seats reserved.

## 7. Project Files

The project is divided into two main parts:

**`backend/`** – Handles admission rules, login, database operations, and API requests.

**`frontend/`** – Contains the website used by students and school administrators.

Other important files:

| File | Purpose |
|---|---|
| `docker-compose.yml` | Starts the complete application. |
| `docker-compose.test.yml` | Runs the automated tests. |
| `backend/Dockerfile` | Builds the backend container. |
| `frontend/Dockerfile` | Builds the frontend container. |
| `README.md` | Project setup and usage instructions. |
| `ASSUMPTIONS.md` | Explains additional decisions made during development. |
| `AI_USAGE.md` | Documents AI tools used during development. |

---

**That's it!**

Start Docker, run `docker compose up --build`, and open http://localhost:3000.

Parents can apply for admission through the student portal, while school staff can manage classes and monitor applications through the admin portal.