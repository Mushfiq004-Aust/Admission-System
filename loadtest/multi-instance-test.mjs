// Optional bonus check: proves seat protection lives in PostgreSQL, not in one process's memory.
// It needs TWO backend processes running against the SAME database.
//
// With Docker (easiest):
//     docker compose --profile bonus up --build       -> backends on :8080 and :8081
// Without Docker (two terminals, PostgreSQL running):
//     terminal 1:  cd backend && dotnet run
//     terminal 2:  cd backend && dotnet run --no-launch-profile --urls http://localhost:8081
//
// Then run (Node 18+):   node loadtest/multi-instance-test.mjs
// (If you changed ADMIN_USERNAME / ADMIN_PASSWORD in .env, set the same variables before running it.)
//
// It creates a class with 10 seats and sends 200 simultaneous applications, alternating between
// the two backends. Expected: exactly 10 x 201 Created, 190 x 409 Conflict, never more than 10 seats taken.
const A = process.env.BACKEND_A ?? "http://localhost:8080";
const B = process.env.BACKEND_B ?? "http://localhost:8081";
const SEATS = Number(process.env.SEATS ?? 10);
const REQUESTS = Number(process.env.REQUESTS ?? 200);

// Creating a class is an admin operation, so log in first (same values as docker-compose.yml / your .env file).
const ADMIN_USERNAME = process.env.ADMIN_USERNAME ?? "admin";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "admin123";
const login = await fetch(`${A}/admin/login`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ username: ADMIN_USERNAME, password: ADMIN_PASSWORD }),
});
if (login.status !== 200) throw new Error(`Admin login failed (${login.status}). Set ADMIN_USERNAME / ADMIN_PASSWORD to match your .env`);
const { token } = await login.json();

const res = await fetch(`${A}/classes`, {
  method: "POST",
  headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
  body: JSON.stringify({ name: `Multi-instance test ${Date.now()}`, totalSeats: SEATS }),
});
if (res.status !== 201) throw new Error(`Could not create class: ${res.status}`);
const cls = await res.json();

let base = 20260100000000000n;
const calls = Array.from({ length: REQUESTS }, (_, i) => {
  const student = (base + BigInt(i)).toString();
  const target = i % 2 === 0 ? A : B; // alternate between the two processes
  return fetch(`${target}/classes/${cls.id}/holds`, { method: "POST", headers: { "X-Student-Id": student } })
    .then((r) => ({ status: r.status, target }));
});
const results = await Promise.all(calls);

const count = (pred) => results.filter(pred).length;
const created = count((r) => r.status === 201);
const conflict = count((r) => r.status === 409);
const other = REQUESTS - created - conflict;

const statsA = await (await fetch(`${A}/classes/${cls.id}`)).json();
const statsB = await (await fetch(`${B}/classes/${cls.id}`)).json();

console.log(`Class ${cls.id}: ${SEATS} seats, ${REQUESTS} simultaneous requests across ${A} and ${B}`);
console.log(`  201 Created : ${created}  (A: ${count((r) => r.status === 201 && r.target === A)}, B: ${count((r) => r.status === 201 && r.target === B)})`);
console.log(`  409 Conflict: ${conflict}`);
console.log(`  other       : ${other}`);
console.log(`  stats via A : held=${statsA.heldSeats} confirmed=${statsA.confirmedSeats} available=${statsA.availableSeats}`);
console.log(`  stats via B : held=${statsB.heldSeats} confirmed=${statsB.confirmedSeats} available=${statsB.availableSeats}`);

const ok = created === SEATS && other === 0 && statsA.heldSeats + statsA.confirmedSeats <= SEATS;
console.log(ok ? "PASS: capacity never exceeded." : "FAIL: invariant violated!");
process.exit(ok ? 0 : 1);
