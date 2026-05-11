import { FormEvent, useCallback, useEffect, useMemo, useState } from 'react';
import {
  Download,
  Headphones,
  Loader2,
  Plus,
  RefreshCw,
  Trash2,
  Users
} from 'lucide-react';
import { api } from './api';
import type {
  GenerationJob,
  OutputFormat,
  ProjectDetails,
  ProjectSummary,
  SpeechSpeed,
  VoiceProfile
} from './types';

const speeds: Array<{ value: SpeechSpeed; label: string }> = [
  { value: 'normal', label: 'Normal' },
  { value: 'slow', label: 'Slow' },
  { value: 'very_slow', label: 'Very slow' }
];

const formats: Array<{ value: OutputFormat; label: string }> = [
  { value: 'mp3', label: 'MP3' },
  { value: 'wav', label: 'WAV' }
];

const languages = [
  { value: '', label: 'Profile default' },
  { value: 'Auto', label: 'Auto' },
  { value: 'English', label: 'English' },
  { value: 'Russian', label: 'Russian' },
  { value: 'Chinese', label: 'Chinese' },
  { value: 'Japanese', label: 'Japanese' },
  { value: 'Korean', label: 'Korean' },
  { value: 'German', label: 'German' },
  { value: 'French', label: 'French' },
  { value: 'Spanish', label: 'Spanish' },
  { value: 'Italian', label: 'Italian' },
  { value: 'Portuguese', label: 'Portuguese' }
];

export function App() {
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [selectedProject, setSelectedProject] = useState<ProjectDetails | null>(null);
  const [voices, setVoices] = useState<VoiceProfile[]>([]);
  const [draftTitle, setDraftTitle] = useState('Rainy Evening Dialogue');
  const [draftText, setDraftText] = useState(
    'Emma: It is raining again, but I like this sound.\nAlex: Me too. It makes the room feel quiet and warm.\nEmma: Let us make tea and read for a while.'
  );
  const [editTitle, setEditTitle] = useState('');
  const [editText, setEditText] = useState('');
  const [voiceProfileCode, setVoiceProfileCode] = useState('');
  const [useDialogueVoices, setUseDialogueVoices] = useState(true);
  const [speakerVoiceProfileCodes, setSpeakerVoiceProfileCodes] = useState<Record<string, string>>({
    Emma: 'cozy_female',
    Alex: 'calm_male'
  });
  const [speed, setSpeed] = useState<SpeechSpeed>('slow');
  const [outputFormat, setOutputFormat] = useState<OutputFormat>('mp3');
  const [language, setLanguage] = useState('');
  const [emotionPrompt, setEmotionPrompt] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const selectedLatestJob = selectedProject?.jobs[0] ?? null;
  const hasRunningJob = selectedProject?.jobs.some(
    (job) => job.status === 'Pending' || job.status === 'Processing'
  );
  const detectedSpeakers = useMemo(() => extractSpeakers(editText), [editText]);
  const scriptStats = useMemo(() => getScriptStats(editText), [editText]);
  const enabledEngines = useMemo(() => summarizeEngines(voices), [voices]);
  const selectedVoice = useMemo(
    () => voices.find((voice) => voice.code === voiceProfileCode) ?? null,
    [voiceProfileCode, voices]
  );
  const dialogueVoiceOptions = useMemo(() => {
    if (!selectedVoice) {
      return voices;
    }

    return voices.filter((voice) => sameEngine(voice, selectedVoice));
  }, [selectedVoice, voices]);

  const loadProjects = useCallback(async () => {
    const items = await api.listProjects();
    setProjects(items);
    return items;
  }, []);

  const loadProject = useCallback(async (id: string) => {
    const project = await api.getProject(id);
    setSelectedProject(project);
    return project;
  }, []);

  useEffect(() => {
    let cancelled = false;

    async function bootstrap() {
      setIsLoading(true);
      setError(null);
      try {
        const [voiceItems, projectItems] = await Promise.all([api.listVoices(), api.listProjects()]);
        if (cancelled) {
          return;
        }

        setVoices(voiceItems);
        setProjects(projectItems);
        setVoiceProfileCode((current) => current || voiceItems[0]?.code || '');
        setSpeakerVoiceProfileCodes((current) => seedSpeakerVoices(current, ['Emma', 'Alex'], voiceItems));

        if (projectItems[0]) {
          await loadProject(projectItems[0].id);
        }
      } catch (err) {
        if (!cancelled) {
          setError(toErrorMessage(err));
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    }

    bootstrap();
    return () => {
      cancelled = true;
    };
  }, [loadProject]);

  useEffect(() => {
    if (!selectedProject?.id || !hasRunningJob) {
      return;
    }

    const timer = window.setInterval(async () => {
      try {
        await Promise.all([loadProject(selectedProject.id), loadProjects()]);
      } catch (err) {
        setError(toErrorMessage(err));
      }
    }, 2500);

    return () => window.clearInterval(timer);
  }, [hasRunningJob, loadProject, loadProjects, selectedProject?.id]);

  useEffect(() => {
    setEditTitle(selectedProject?.title ?? '');
    setEditText(selectedProject?.sourceText ?? '');
  }, [selectedProject?.id, selectedProject?.sourceText, selectedProject?.title]);

  useEffect(() => {
    if (voices.length === 0 || detectedSpeakers.length === 0) {
      return;
    }

    setSpeakerVoiceProfileCodes((current) =>
      seedSpeakerVoices(current, detectedSpeakers, dialogueVoiceOptions, voiceProfileCode)
    );
  }, [detectedSpeakers, dialogueVoiceOptions, voiceProfileCode, voices.length]);

  const canGenerate = useMemo(
    () => Boolean(selectedProject && voiceProfileCode),
    [selectedProject, voiceProfileCode]
  );

  async function createProject(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setError(null);
    try {
      const project = await api.createProject({ title: draftTitle, sourceText: draftText });
      setSelectedProject(project);
      await loadProjects();
    } catch (err) {
      setError(toErrorMessage(err));
    } finally {
      setIsSaving(false);
    }
  }

  async function selectProject(id: string) {
    setError(null);
    try {
      await loadProject(id);
    } catch (err) {
      setError(toErrorMessage(err));
    }
  }

  async function deleteSelectedProject() {
    if (!selectedProject) {
      return;
    }

    setError(null);
    try {
      await api.deleteProject(selectedProject.id);
      setSelectedProject(null);
      const items = await loadProjects();
      if (items[0]) {
        await loadProject(items[0].id);
      }
    } catch (err) {
      setError(toErrorMessage(err));
    }
  }

  async function updateSelectedProject() {
    if (!selectedProject) {
      return;
    }

    setIsUpdating(true);
    setError(null);
    try {
      const project = await api.updateProject(selectedProject.id, {
        title: editTitle,
        sourceText: editText
      });
      setSelectedProject(project);
      await loadProjects();
    } catch (err) {
      setError(toErrorMessage(err));
    } finally {
      setIsUpdating(false);
    }
  }

  async function generateAudio() {
    if (!selectedProject) {
      return;
    }

    setIsGenerating(true);
    setError(null);
    try {
      await api.generate(selectedProject.id, {
        voiceProfileCode,
        speed,
        outputFormat,
        language: language || undefined,
        emotionPrompt: emotionPrompt.trim() || undefined,
        useDialogueVoices,
        speakerVoiceProfileCodes: useDialogueVoices
          ? buildSpeakerVoicePayload(detectedSpeakers, speakerVoiceProfileCodes, voiceProfileCode)
          : undefined
      });
      await Promise.all([loadProject(selectedProject.id), loadProjects()]);
    } catch (err) {
      setError(toErrorMessage(err));
    } finally {
      setIsGenerating(false);
    }
  }

  async function retryJob(job: GenerationJob) {
    setError(null);
    try {
      await api.retry(job.id);
      await Promise.all([loadProject(job.projectId), loadProjects()]);
    } catch (err) {
      setError(toErrorMessage(err));
    }
  }

  return (
    <main className="app-shell">
      <header className="app-header">
        <div className="brand">
          <div className="brand-mark">
            <Headphones size={20} />
          </div>
          <div>
            <h1>Cozy TTS</h1>
            <p>Local voice generator</p>
          </div>
        </div>

        <div className="header-meta" aria-label="System status">
          <span>{enabledEngines.length > 0 ? enabledEngines.join(' / ') : 'No engines'}</span>
          {selectedLatestJob ? (
            <span className={`status-label ${selectedLatestJob.status.toLowerCase()}`}>
              {selectedLatestJob.status}
            </span>
          ) : null}
          {selectedLatestJob?.audioFile?.durationSeconds ? (
            <span>{formatDuration(selectedLatestJob.audioFile.durationSeconds)}</span>
          ) : null}
        </div>
      </header>

      {error ? <div className="error-panel">{error}</div> : null}

      <section className="app-layout">
        <aside className="sidebar">
          <form className="panel new-project" onSubmit={createProject}>
            <SectionTitle title="New project" />
            <label>
              <span>Title</span>
              <input value={draftTitle} onChange={(event) => setDraftTitle(event.target.value)} />
            </label>
            <label>
              <span>Script</span>
              <textarea
                rows={6}
                value={draftText}
                onChange={(event) => setDraftText(event.target.value)}
              />
            </label>
            <button className="primary-button" type="submit" disabled={isSaving}>
              {isSaving ? <Loader2 className="spin" size={17} /> : <Plus size={17} />}
              Create
            </button>
          </form>

          <section className="panel projects-panel" aria-label="Projects">
            <SectionTitle title="Projects" detail={`${projects.length}`} />
            <div className="project-list">
              {projects.map((project) => (
                <button
                  key={project.id}
                  className={`project-row ${project.id === selectedProject?.id ? 'active' : ''}`}
                  type="button"
                  onClick={() => selectProject(project.id)}
                >
                  <span>{project.title}</span>
                  <small>{project.jobsCount} renders</small>
                </button>
              ))}
              {!isLoading && projects.length === 0 ? <p className="empty">No projects yet</p> : null}
            </div>
          </section>
        </aside>

        {selectedProject ? (
          <>
            <section className="workspace">
              <section className="panel editor-panel">
                <div className="panel-title-row">
                  <div>
                    <SectionTitle title="Script" />
                    <h2>{selectedProject.title}</h2>
                  </div>
                  <button
                    className="icon-button danger"
                    type="button"
                    onClick={deleteSelectedProject}
                    title="Delete project"
                  >
                    <Trash2 size={18} />
                  </button>
                </div>

                <div className="stats-row" aria-label="Script metrics">
                  <span>{scriptStats.characters.toLocaleString()} chars</span>
                  <span>{scriptStats.words.toLocaleString()} words</span>
                  <span>{detectedSpeakers.length || 1} speaker{(detectedSpeakers.length || 1) === 1 ? '' : 's'}</span>
                </div>

                <label>
                  <span>Project title</span>
                  <input value={editTitle} onChange={(event) => setEditTitle(event.target.value)} />
                </label>
                <label>
                  <span>Text</span>
                  <textarea
                    className="source-view"
                    value={editText}
                    onChange={(event) => setEditText(event.target.value)}
                  />
                </label>

                <div className="detected-speakers" aria-label="Detected speakers">
                  {(detectedSpeakers.length > 0 ? detectedSpeakers : ['Narrator']).map((speaker) => (
                    <span key={speaker}>
                      <Users size={14} />
                      {speaker}
                    </span>
                  ))}
                </div>

                <button
                  className="secondary-button save-changes"
                  type="button"
                  disabled={isUpdating}
                  onClick={updateSelectedProject}
                >
                  {isUpdating ? <Loader2 className="spin" size={17} /> : <RefreshCw size={17} />}
                  Save changes
                </button>
              </section>

              <section className="panel history-panel">
                <SectionTitle title="History" detail={`${selectedProject.jobs.length} jobs`} />
                <div className="jobs">
                  {selectedProject.jobs.map((job) => (
                    <JobRow key={job.id} job={job} onRetry={retryJob} />
                  ))}
                  {selectedProject.jobs.length === 0 ? <p className="empty">No renders yet</p> : null}
                </div>
              </section>
            </section>

            <aside className="panel settings-panel">
              <SectionTitle title="Voice" detail={selectedVoice ? engineLabel(selectedVoice.engine) : undefined} />

              <label>
                <span>Voice profile</span>
                <select value={voiceProfileCode} onChange={(event) => setVoiceProfileCode(event.target.value)}>
                  {voices.map((voice) => (
                    <option key={voice.id} value={voice.code}>
                      {formatVoiceLabel(voice)}
                    </option>
                  ))}
                </select>
              </label>

              {selectedVoice ? (
                <p className="field-note">
                  {[
                    engineLabel(selectedVoice.engine),
                    selectedVoice.qwenLanguage,
                    selectedVoice.qwenSpeaker
                  ].filter(Boolean).join(' - ')}
                </p>
              ) : null}

              <div className="field-grid">
                <label>
                  <span>Language</span>
                  <select value={language} onChange={(event) => setLanguage(event.target.value)}>
                    {languages.map((item) => (
                      <option key={item.value || 'profile'} value={item.value}>
                        {item.label}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  <span>Format</span>
                  <select value={outputFormat} onChange={(event) => setOutputFormat(event.target.value as OutputFormat)}>
                    {formats.map((item) => (
                      <option key={item.value} value={item.value}>
                        {item.label}
                      </option>
                    ))}
                  </select>
                </label>
              </div>

              <label>
                <span>Style note</span>
                <textarea
                  className="emotion-input"
                  rows={3}
                  value={emotionPrompt}
                  placeholder="warm, calm, smiling"
                  onChange={(event) => setEmotionPrompt(event.target.value)}
                />
              </label>

              <div className="choice-row" aria-label="Speech speed">
                {speeds.map((item) => (
                  <button
                    key={item.value}
                    className={speed === item.value ? 'selected' : ''}
                    type="button"
                    onClick={() => setSpeed(item.value)}
                  >
                    {item.label}
                  </button>
                ))}
              </div>

              <label className="checkbox-row">
                <input
                  type="checkbox"
                  checked={useDialogueVoices}
                  onChange={(event) => setUseDialogueVoices(event.target.checked)}
                />
                <span>Separate voices for dialogue</span>
              </label>

              {useDialogueVoices ? (
                <div className="cast-panel">
                  <SectionTitle title="Cast" />
                  {(detectedSpeakers.length > 0 ? detectedSpeakers : ['Emma', 'Alex']).map((speaker) => (
                    <label key={speaker} className="speaker-row">
                      <span>{speaker}</span>
                      <select
                        value={getSpeakerVoiceValue(
                          speaker,
                          speakerVoiceProfileCodes,
                          voiceProfileCode,
                          dialogueVoiceOptions
                        )}
                        onChange={(event) =>
                          setSpeakerVoiceProfileCodes((current) => ({
                            ...current,
                            [speaker]: event.target.value
                          }))
                        }
                      >
                        {dialogueVoiceOptions.map((voice) => (
                          <option key={voice.id} value={voice.code}>
                            {formatVoiceLabel(voice)}
                          </option>
                        ))}
                      </select>
                    </label>
                  ))}
                </div>
              ) : null}

              <button
                className="primary-button generate"
                type="button"
                disabled={!canGenerate || isGenerating}
                onClick={generateAudio}
              >
                {isGenerating ? <Loader2 className="spin" size={17} /> : null}
                Generate
              </button>

              {selectedLatestJob ? <JobStatusBlock job={selectedLatestJob} /> : null}
            </aside>
          </>
        ) : (
          <div className="panel empty-state">
            <Headphones size={34} />
            <p>Create a project to start generating audio.</p>
          </div>
        )}
      </section>
    </main>
  );
}

function SectionTitle({ title, detail }: { title: string; detail?: string }) {
  return (
    <div className="section-title">
      <span>{title}</span>
      {detail ? <small>{detail}</small> : null}
    </div>
  );
}

function JobStatusBlock({ job }: { job: GenerationJob }) {
  return (
    <div className="status-block">
      <div className="status-line">
        <span className={`status-dot ${job.status.toLowerCase()}`} />
        <strong>{job.status}</strong>
        <span>{job.voiceProfileName || job.voiceProfileCode}</span>
        {job.language ? <span>{job.language}</span> : null}
        {job.useDialogueVoices ? <span>Dialogue</span> : null}
      </div>

      {job.emotionPrompt ? <p className="job-style">{job.emotionPrompt}</p> : null}
      {job.errorMessage ? <p className="job-error">{job.errorMessage}</p> : null}
      {job.status === 'Completed' ? (
        <div className="player">
          <audio controls src={api.audioUrl(job.id)} />
          <a className="download-link" href={api.downloadUrl(job.id)}>
            <Download size={16} />
            Download
          </a>
        </div>
      ) : null}
    </div>
  );
}

function JobRow({ job, onRetry }: { job: GenerationJob; onRetry: (job: GenerationJob) => void }) {
  const canRetry = job.status === 'Completed' || job.status === 'Failed';

  return (
    <article className="job-row">
      <div className="job-body">
        <div className="job-meta">
          <span className={`status-pill ${job.status.toLowerCase()}`}>{job.status}</span>
          <span>{job.voiceProfileName || job.voiceProfileCode}</span>
          {job.useDialogueVoices ? <span>dialogue</span> : null}
          {job.language ? <span>{job.language}</span> : null}
          <span>{job.speed.replace('_', ' ')}</span>
          <span>{job.outputFormat.toUpperCase()}</span>
          {job.audioFile?.durationSeconds ? <span>{formatDuration(job.audioFile.durationSeconds)}</span> : null}
        </div>
        <time>{new Date(job.createdAt).toLocaleString()}</time>
        {job.emotionPrompt ? <p className="job-style">{job.emotionPrompt}</p> : null}
        {job.errorMessage ? <p className="job-error">{job.errorMessage}</p> : null}
      </div>
      <div className="job-actions">
        {job.status === 'Completed' ? (
          <a className="icon-button" href={api.downloadUrl(job.id)} title="Download">
            <Download size={17} />
          </a>
        ) : null}
        <button className="icon-button" type="button" disabled={!canRetry} onClick={() => onRetry(job)} title="Retry">
          <RefreshCw size={17} />
        </button>
      </div>
    </article>
  );
}

function toErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : 'Unexpected error';
}

function extractSpeakers(text: string) {
  const speakers: string[] = [];
  const seen = new Set<string>();
  const regex = /^(\p{L}[\p{L}\p{N} _.'-]{0,48})\s*:/u;

  for (const line of text.split(/\r?\n/)) {
    const match = regex.exec(line.trim());
    if (!match) {
      continue;
    }

    const speaker = match[1].trim().replace(/\s+/g, ' ');
    const key = speaker.toLowerCase();
    if (!seen.has(key)) {
      seen.add(key);
      speakers.push(speaker);
    }
  }

  return speakers;
}

function getScriptStats(text: string) {
  const trimmed = text.trim();
  return {
    characters: trimmed.length,
    words: trimmed ? trimmed.split(/\s+/).length : 0
  };
}

function summarizeEngines(voices: VoiceProfile[]) {
  const engines = new Set<string>();
  for (const voice of voices) {
    engines.add(engineLabel(voice.engine));
  }

  return Array.from(engines);
}

function seedSpeakerVoices(
  current: Record<string, string>,
  speakers: string[],
  voices: VoiceProfile[],
  fallbackVoiceCode?: string
) {
  const next = { ...current };
  const fallback = fallbackVoiceCode || voices[0]?.code || '';

  for (const speaker of speakers) {
    if (next[speaker] && voices.some((voice) => voice.code === next[speaker])) {
      continue;
    }

    next[speaker] = pickVoiceForSpeaker(speaker, voices, fallback);
  }

  if (!next.Emma && voices.some((voice) => voice.code === 'cozy_female')) {
    next.Emma = 'cozy_female';
  }

  if (!next.Alex && voices.some((voice) => voice.code === 'calm_male')) {
    next.Alex = 'calm_male';
  }

  return next;
}

function pickVoiceForSpeaker(speaker: string, voices: VoiceProfile[], fallback: string) {
  const lower = speaker.toLowerCase();
  if (/(emma|anna|amy|sarah|mary|kate|lisa)/.test(lower) && voices.some((voice) => voice.code === 'cozy_female')) {
    return 'cozy_female';
  }

  if (/(alex|john|tom|mark|david|ryan)/.test(lower) && voices.some((voice) => voice.code === 'calm_male')) {
    return 'calm_male';
  }

  return fallback;
}

function sameEngine(left: VoiceProfile, right: VoiceProfile) {
  return (left.engine || 'piper').toLowerCase() === (right.engine || 'piper').toLowerCase();
}

function engineLabel(engine: string) {
  return engine.toLowerCase() === 'qwen3' ? 'Qwen3' : 'Piper';
}

function formatVoiceLabel(voice: VoiceProfile) {
  return `${voice.displayName} - ${engineLabel(voice.engine)}`;
}

function formatDuration(seconds: number) {
  const safeSeconds = Math.max(0, Math.round(seconds));
  const minutes = Math.floor(safeSeconds / 60);
  const remainingSeconds = safeSeconds % 60;
  return `${minutes}:${remainingSeconds.toString().padStart(2, '0')}`;
}

function getSpeakerVoiceValue(
  speaker: string,
  speakerVoiceProfileCodes: Record<string, string>,
  fallbackVoiceCode: string,
  voices: VoiceProfile[]
) {
  const selected = speakerVoiceProfileCodes[speaker] || fallbackVoiceCode;
  return voices.some((voice) => voice.code === selected) ? selected : fallbackVoiceCode;
}

function buildSpeakerVoicePayload(
  speakers: string[],
  speakerVoiceProfileCodes: Record<string, string>,
  fallbackVoiceCode: string
) {
  const payload: Record<string, string> = {};
  for (const speaker of speakers) {
    payload[speaker] = speakerVoiceProfileCodes[speaker] || fallbackVoiceCode;
  }

  return payload;
}
