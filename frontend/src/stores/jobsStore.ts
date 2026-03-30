import { create } from 'zustand';
import type { JobSearchRequest } from '../api/jobs';

interface JobsState {
  searchParams: JobSearchRequest;
  setSearchParams: (params: Partial<JobSearchRequest>) => void;
}

export const useJobsStore = create<JobsState>((set) => ({
  searchParams: {
    page: 1,
    pageSize: 20,
    sortBy: 'date',
    sortDescending: true,
  },

  setSearchParams: (params) => {
    set((state) => ({
      searchParams: { ...state.searchParams, ...params },
    }));
  },
}));
