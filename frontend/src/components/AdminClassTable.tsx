import type { AdmissionClass } from "../types";

export function AdminClassTable({ classes }: { classes: AdmissionClass[] }) {
  return (
    <div className="table-wrap">
      <table>
        <thead>
          <tr><th>Class / shift</th><th className="num">Total</th><th className="num">Held</th><th className="num">Confirmed</th><th className="num">Available</th></tr>
        </thead>
        <tbody>
          {classes.map((c) => (
            <tr key={c.id}>
              <td>{c.name}</td>
              <td className="num">{c.totalSeats}</td>
              <td className="num">{c.heldSeats}</td>
              <td className="num">{c.confirmedSeats}</td>
              <td className="num"><strong className={c.availableSeats === 0 ? "text-red" : undefined}>{c.availableSeats}</strong></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
