export type JobStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
export type SpeechSpeed = 'very_slow' | 'slow' | 'normal';
export type OutputFormat = 'mp3' | 'wav';

export interface AudioFile {
  id: string;
  jobId: string;
  fileName: string;
  filePath: string;
  mimeType: string;
  sizeBytes: number;
  durationSeconds: number | null;
  createdAt: string;
}

export interface GenerationJob {
  id: string;
  projectId: string;
  status: JobStatus;
  voiceProfileId: string;
  voiceProfileCode: string;
  voiceProfileName: string;
  speed: SpeechSpeed;
  outputFormat: OutputFormat;
  language: string | null;
  emotionPrompt: string | null;
  useDialogueVoices: boolean;
  speakerVoiceProfileCodes: Record<string, string>;
  errorMessage: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  audioFile: AudioFile | null;
}

export interface ProjectSummary {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  jobsCount: number;
  latestJob: GenerationJob | null;
}

export interface ProjectDetails {
  id: string;
  title: string;
  sourceText: string;
  createdAt: string;
  updatedAt: string;
  jobs: GenerationJob[];
}

export interface VoiceProfile {
  id: string;
  code: string;
  displayName: string;
  engine: string;
  piperModelPath: string;
  piperConfigPath: string | null;
  qwenModel: string | null;
  qwenMode: string | null;
  qwenSpeaker: string | null;
  qwenLanguage: string | null;
  qwenInstruction: string | null;
  description: string | null;
}

export interface CreateProjectPayload {
  title: string;
  sourceText: string;
}

export interface GeneratePayload {
  voiceProfileCode: string;
  speed: SpeechSpeed;
  outputFormat: OutputFormat;
  language?: string;
  emotionPrompt?: string;
  useDialogueVoices: boolean;
  speakerVoiceProfileCodes?: Record<string, string>;
}
