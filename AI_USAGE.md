# AI Usage

## Tool Used
**Claude (Anthropic)**, used through its chat interface.

## How I Used AI
- **Frontend:** I used Claude extensively to build the React frontend because I am not very familiar with React.
- **Backend:** I developed the backend based on my experience building my previous project, **Mukhosh**. I followed a similar structure and approach, using Controllers, Services, Repositories, DTOs, and other layers. I used AI for explanations, suggestions, and reviewing some implementation details.
- **Documentation:** I used AI to help prepare and organize the project documentation.

## My Contribution
The backend structure, admission requirements, business rules, and overall project decisions were based on my own understanding and previous experience with Mukhosh. AI was mainly a development aid, particularly for the React frontend.

I reviewed the generated code and made decisions about how the system should work. I am responsible for understanding and explaining the final implementation.

## Verification
I personally ran the project using Docker and verified the backend tests, frontend tests, and the two-instance concurrency test.

- Backend tests: 49 passed
- Frontend tests: 18 passed
- Two-instance load test: 200 simultaneous requests across two backend instances; exactly 10 applications succeeded for a class with 10 seats.
