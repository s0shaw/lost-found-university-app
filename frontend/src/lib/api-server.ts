import { cookies } from "next/headers";
import { api } from "./api";

/** Server components only: forwards the visitor's cookies to the API.
    A request made from a server component leaves the Next server, not the browser, so the
    httpOnly staff cookie is not attached by itself. `next/headers` cannot be imported from a
    client component, which is why this lives apart from `api()`. */
export async function apiServer<T>(path: string, init?: RequestInit): Promise<T> {
  const cookie = (await cookies()).toString();
  return api<T>(path, { ...init, headers: { ...init?.headers, cookie } });
}
