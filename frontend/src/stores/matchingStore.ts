import { create } from 'zustand';
import type { RecommendationsRequest } from '../api/matching';

interface MatchingState {
  params: RecommendationsRequest;
  setParams: (params: Partial<RecommendationsRequest>) => void;
}

export const useMatchingStore = create<MatchingState>((set) => ({
  params: {
    page: 1,
    pageSize: 20,
    minScore: 0.3,
  },

  setParams: (params) => {
    set((state) => ({
      params: { ...state.params, ...params },
    }));
  },
}));
