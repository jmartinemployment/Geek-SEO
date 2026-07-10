const DB_NAME = "content-writer-export";
const STORE_NAME = "handles";
const HANDLE_KEY = "output-root";

function openDb(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB_NAME, 1);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve(request.result);
    request.onupgradeneeded = () => {
      request.result.createObjectStore(STORE_NAME);
    };
  });
}

async function readStoredHandle(): Promise<FileSystemDirectoryHandle | null> {
  if (typeof window === "undefined" || !("showDirectoryPicker" in window)) return null;

  const db = await openDb();
  return new Promise((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readonly");
    const request = tx.objectStore(STORE_NAME).get(HANDLE_KEY);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve((request.result as FileSystemDirectoryHandle | undefined) ?? null);
  });
}

async function storeHandle(handle: FileSystemDirectoryHandle): Promise<void> {
  const db = await openDb();
  await new Promise<void>((resolve, reject) => {
    const tx = db.transaction(STORE_NAME, "readwrite");
    const request = tx.objectStore(STORE_NAME).put(handle, HANDLE_KEY);
    request.onerror = () => reject(request.error);
    request.onsuccess = () => resolve();
  });
}

async function ensureWritePermission(handle: FileSystemDirectoryHandle): Promise<boolean> {
  const current = await handle.queryPermission({ mode: "readwrite" });
  if (current === "granted") return true;
  const requested = await handle.requestPermission({ mode: "readwrite" });
  return requested === "granted";
}

export function canWriteExportToLocalDirectory(): boolean {
  return typeof window !== "undefined" && "showDirectoryPicker" in window;
}

export async function pickExportDirectory(): Promise<FileSystemDirectoryHandle> {
  if (!canWriteExportToLocalDirectory()) {
    throw new Error("This browser cannot write export files to a folder on your Mac.");
  }

  const handle = await window.showDirectoryPicker({
    mode: "readwrite",
    startIn: "documents",
  });
  await storeHandle(handle);
  return handle;
}

export async function resolveExportDirectory(): Promise<FileSystemDirectoryHandle> {
  if (!canWriteExportToLocalDirectory()) {
    throw new Error("This browser cannot write export files to a folder on your Mac.");
  }

  const stored = await readStoredHandle();
  if (stored && (await ensureWritePermission(stored))) {
    return stored;
  }

  return pickExportDirectory();
}

export async function writeExportFilesToDirectory(
  root: FileSystemDirectoryHandle,
  files: { relativePath: string; markdown: string }[],
): Promise<void> {
  for (const file of files) {
    const parts = file.relativePath.split("/").filter(Boolean);
    if (parts.length === 0) continue;

    let dir = root;
    for (const segment of parts.slice(0, -1)) {
      dir = await dir.getDirectoryHandle(segment, { create: true });
    }

    const fileName = parts[parts.length - 1];
    const fileHandle = await dir.getFileHandle(fileName, { create: true });
    const writable = await fileHandle.createWritable();
    await writable.write(file.markdown);
    await writable.close();
  }
}
