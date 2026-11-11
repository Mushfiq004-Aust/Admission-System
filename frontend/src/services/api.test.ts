import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, api } from "./api";

function mockFetch(status: number, body?: unknown) {
  const fn = vi.fn().mockImplementation(() =>
    Promise.resolve(new Response(body === undefined ? null : JSON.stringify(body), { status })),
  );
  vi.stubGlobal("fetch", fn);
  return fn;
}

function sentHeaders(fn: ReturnType<typeof vi.fn>): Record<string, string> {
  return (fn.mock.calls[0][1] as { headers: Record<string, string> }).headers;
}

afterEach(() => vi.unstubAllGlobals());

describe("api service", () => {
  it("sends the admin token only when creating a class", async () => {
    const fetchMock = mockFetch(201, { id: "1", name: "Class 6", totalSeats: 10 });
    await api.createClass("Class 6", 10, "abc.def.ghi");

    const headers = sentHeaders(fetchMock);
    expect(headers["Authorization"]).toBe("Bearer abc.def.ghi");
    expect(headers["X-Student-Id"]).toBeUndefined();
    expect(String(fetchMock.mock.calls[0][0])).toMatch(/\/classes$/);
  });

  it("does not send any admin credentials for student calls", async () => {
    const listMock = mockFetch(200, []);
    await api.listClasses();
    expect(sentHeaders(listMock)["Authorization"]).toBeUndefined();
    vi.unstubAllGlobals();

    const applyMock = mockFetch(201, { holdId: "h" });
    await api.apply("c1", "20150101234567890");
    const headers = sentHeaders(applyMock);
    expect(headers["X-Student-Id"]).toBe("20150101234567890");
    expect(headers["Authorization"]).toBeUndefined();
  });

  it("turns a 401 into an ApiError with a helpful message", async () => {
    mockFetch(401, { error: "UNAUTHORIZED", message: "Admin login required." });
    const err = await api.createClass("X", 1, "bad").catch((e) => e);

    expect(err).toBeInstanceOf(ApiError);
    expect(err.status).toBe(401);
    expect(err.code).toBe("UNAUTHORIZED");
    expect(err.message).toMatch(/log in again/i);
  });

  it("shows a friendly message when a class is full (409)", async () => {
    mockFetch(409, { error: "CLASS_FULL", message: "No seats are available in this class." });
    const err = await api.apply("c1", "20150101234567890").catch((e) => e);
    expect(err.status).toBe(409);
    expect(err.message).toMatch(/no seats/i);
  });

  it("explains a network failure instead of crashing", async () => {
    vi.stubGlobal("fetch", vi.fn().mockRejectedValue(new TypeError("Failed to fetch")));
    const err = await api.listClasses().catch((e) => e);
    expect(err).toBeInstanceOf(ApiError);
    expect(err.code).toBe("NETWORK");
  });

  it("returns nothing for 204 No Content (cancel)", async () => {
    mockFetch(204);
    await expect(api.cancelHold("h1", "20150101234567890")).resolves.toBeUndefined();
  });
});
