import { create } from 'zustand';
import { matchingApi } from '../api/matching';
import type { RecommendationsResponse, RecommendationsRequest } from '../api/matching';

interface MatchingState {
  results: RecommendationsResponse | null;
  isLoading: boolean;
  error: string | null;
  params: RecommendationsRequest;

  setParams: (params: Partial<RecommendationsRequest>) => void;
  loadRecommendations: (params?: RecommendationsRequest) => Promise<void>;
  clearError: () => void;
}

export const useMatchingStore = create<MatchingState>((set, get) => ({
  results: null,
  isLoading: false,
  error: null,
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

  loadRecommendations: async (params) => {
    set({ isLoading: true, error: null });
    try {
      const requestParams = params || get().params;
      const results = await matchingApi.getRecommendations(requestParams);
      set({ results, params: requestParams });
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Ett fel uppstod vid hämtning av rekommendationer' });
    } finally {
      set({ isLoading: false });
    }
  },

  clearError: () => set({ error: null }),
}));
