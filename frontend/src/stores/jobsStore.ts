import { create } from 'zustand';
import { jobsApi } from '../api/jobs';
import type {
  JobResponse,
  JobSearchRequest,
  JobSearchResponse,
  SavedJobResponse,
  JobStatisticsResponse,
  SavedJobStatus,
} from '../api/jobs';

interface JobsState {
  searchResults: JobSearchResponse | null;
  selectedJob: JobResponse | null;
  savedJobs: SavedJobResponse[];
  statistics: JobStatisticsResponse | null;
  searchParams: JobSearchRequest;
  isLoading: boolean;
  error: string | null;

  setSearchParams: (params: Partial<JobSearchRequest>) => void;
  searchJobs: (params?: JobSearchRequest) => Promise<void>;
  loadJob: (id: string) => Promise<void>;
  loadSavedJobs: () => Promise<void>;
  loadStatistics: () => Promise<void>;
  saveJob: (jobId: string, notes?: string) => Promise<void>;
  updateSavedJob: (jobId: string, status: SavedJobStatus, notes?: string) => Promise<void>;
  removeSavedJob: (jobId: string) => Promise<void>;
  clearSelectedJob: () => void;
  clearError: () => void;
}

export const useJobsStore = create<JobsState>((set, get) => ({
  searchResults: null,
  selectedJob: null,
  savedJobs: [],
  statistics: null,
  searchParams: {
    page: 1,
    pageSize: 20,
    sortBy: 'date',
    sortDescending: true,
  },
  isLoading: false,
  error: null,

  setSearchParams: (params) => {
    set((state) => ({
      searchParams: { ...state.searchParams, ...params },
    }));
  },

  searchJobs: async (params) => {
    set({ isLoading: true, error: null });
    try {
      const searchParams = params || get().searchParams;
      const results = await jobsApi.searchJobs(searchParams);
      set({ searchResults: results, searchParams });
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod vid sökning' });
    } finally {
      set({ isLoading: false });
    }
  },

  loadJob: async (id) => {
    set({ isLoading: true, error: null });
    try {
      const job = await jobsApi.getJob(id);
      set({ selectedJob: job });
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod' });
    } finally {
      set({ isLoading: false });
    }
  },

  loadSavedJobs: async () => {
    set({ isLoading: true, error: null });
    try {
      const savedJobs = await jobsApi.getSavedJobs();
      set({ savedJobs });
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod' });
    } finally {
      set({ isLoading: false });
    }
  },

  loadStatistics: async () => {
    set({ isLoading: true, error: null });
    try {
      const statistics = await jobsApi.getStatistics();
      set({ statistics });
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod' });
    } finally {
      set({ isLoading: false });
    }
  },

  saveJob: async (jobId, notes) => {
    set({ isLoading: true, error: null });
    try {
      await jobsApi.saveJob(jobId, { notes });

      // Update search results
      const searchResults = get().searchResults;
      if (searchResults) {
        const updatedJobs = searchResults.jobs.map((job) =>
          job.id === jobId ? { ...job, isSaved: true, savedStatus: 'Saved' as SavedJobStatus } : job
        );
        set({ searchResults: { ...searchResults, jobs: updatedJobs } });
      }

      // Update selected job
      const selectedJob = get().selectedJob;
      if (selectedJob?.id === jobId) {
        set({ selectedJob: { ...selectedJob, isSaved: true, savedStatus: 'Saved' } });
      }

      // Reload saved jobs
      await get().loadSavedJobs();
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod' });
    } finally {
      set({ isLoading: false });
    }
  },

  updateSavedJob: async (jobId, status, notes) => {
    set({ isLoading: true, error: null });
    try {
      await jobsApi.updateSavedJob(jobId, { status, notes });

      // Update saved jobs list
      const savedJobs = get().savedJobs.map((saved) =>
        saved.job.id === jobId ? { ...saved, status, notes: notes ?? saved.notes } : saved
      );
      set({ savedJobs });

      // Update search results
      const searchResults = get().searchResults;
      if (searchResults) {
        const updatedJobs = searchResults.jobs.map((job) =>
          job.id === jobId ? { ...job, savedStatus: status } : job
        );
        set({ searchResults: { ...searchResults, jobs: updatedJobs } });
      }

      // Update selected job
      const selectedJob = get().selectedJob;
      if (selectedJob?.id === jobId) {
        set({ selectedJob: { ...selectedJob, savedStatus: status } });
      }
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod' });
    } finally {
      set({ isLoading: false });
    }
  },

  removeSavedJob: async (jobId) => {
    set({ isLoading: true, error: null });
    try {
      await jobsApi.removeSavedJob(jobId);

      // Update saved jobs list
      const savedJobs = get().savedJobs.filter((saved) => saved.job.id !== jobId);
      set({ savedJobs });

      // Update search results
      const searchResults = get().searchResults;
      if (searchResults) {
        const updatedJobs = searchResults.jobs.map((job) =>
          job.id === jobId ? { ...job, isSaved: false, savedStatus: undefined } : job
        );
        set({ searchResults: { ...searchResults, jobs: updatedJobs } });
      }

      // Update selected job
      const selectedJob = get().selectedJob;
      if (selectedJob?.id === jobId) {
        set({ selectedJob: { ...selectedJob, isSaved: false, savedStatus: undefined } });
      }
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod' });
    } finally {
      set({ isLoading: false });
    }
  },

  clearSelectedJob: () => set({ selectedJob: null }),
  clearError: () => set({ error: null }),
}));
