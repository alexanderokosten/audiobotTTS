import type {
  CreateProjectPayload,
  GeneratePayload,
  GenerationJob,
  ProjectDetails,
  ProjectSummary,
  VoiceProfile
} from './types';

const configuredBase = import.meta.env.VITE_API_BASE_URL ?? '';
const API_BASE = configuredBase.replace(/\/$/, '');

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {})
    }
  });

  if (!response.ok) {
    let message = `Request failed with ${response.status}`;
    try {
      const problem = await response.json();
      message = problem.detail || problem.title || message;
    } catch {
      // Ignore non-JSON error payloads.
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  listProjects: () => request<ProjectSummary[]>('/api/projects'),
  getProject: (id: string) => request<ProjectDetails>(`/api/projects/${id}`),
  createProject: (payload: CreateProjectPayload) =>
    request<ProjectDetails>('/api/projects', {
      method: 'POST',
      body: JSON.stringify(payload)
    }),
  updateProject: (id: string, payload: CreateProjectPayload) =>
    request<ProjectDetails>(`/api/projects/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    }),
  deleteProject: (id: string) =>
    request<void>(`/api/projects/${id}`, {
      method: 'DELETE'
    }),
  listVoices: () => request<VoiceProfile[]>('/api/voices'),
  generate: (projectId: string, payload: GeneratePayload) =>
    request<GenerationJob>(`/api/projects/${projectId}/generate`, {
      method: 'POST',
      body: JSON.stringify(payload)
    }),
  getJob: (jobId: string) => request<GenerationJob>(`/api/jobs/${jobId}`),
  retry: (jobId: string) =>
    request<GenerationJob>(`/api/jobs/${jobId}/retry`, {
      method: 'POST'
    }),
  audioUrl: (jobId: string) => `${API_BASE}/api/jobs/${jobId}/audio`,
  downloadUrl: (jobId: string) => `${API_BASE}/api/jobs/${jobId}/download`
};
