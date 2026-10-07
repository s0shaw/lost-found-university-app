import { api } from "@/lib/api";
import type { Category, Location } from "./types";

export const listCategories = () => api<Category[]>("/api/categories");
export const listLocations = () => api<Location[]>("/api/locations");
