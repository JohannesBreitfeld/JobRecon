import { create } from 'zustand';
import { profileApi } from '../api/profile';
import type {
  ProfileResponse,
  CreateProfileRequest,
  UpdateProfileRequest,
  AddSkillRequest,
  UpdateJobPreferenceRequest,
  SkillResponse,
  JobPreferenceResponse,
  CVDocumentResponse,
} from '../api/profile';

interface ProfileState {
  profile: ProfileResponse | null;
  isLoading: boolean;
  error: string | null;

  fetchProfile: () => Promise<void>;
  createProfile: (request: CreateProfileRequest) => Promise<void>;
  updateProfile: (request: UpdateProfileRequest) => Promise<void>;
  addSkill: (request: AddSkillRequest) => Promise<SkillResponse>;
  removeSkill: (skillId: string) => Promise<void>;
  updatePreferences: (request: UpdateJobPreferenceRequest) => Promise<JobPreferenceResponse>;
  uploadCV: (file: File) => Promise<CVDocumentResponse>;
  deleteCV: (documentId: string) => Promise<void>;
  setPrimaryCV: (documentId: string) => Promise<void>;
  clearError: () => void;
  reset: () => void;
}

export const useProfileStore = create<ProfileState>((set, get) => ({
  profile: null,
  isLoading: false,
  error: null,

  fetchProfile: async () => {
    set({ isLoading: true, error: null });
    try {
      const profile = await profileApi.getProfile();
      set({ profile, isLoading: false });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte hämta profil';
      set({ error: message, isLoading: false });
    }
  },

  createProfile: async (request: CreateProfileRequest) => {
    set({ isLoading: true, error: null });
    try {
      const profile = await profileApi.createProfile(request);
      set({ profile, isLoading: false });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte skapa profil';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  updateProfile: async (request: UpdateProfileRequest) => {
    set({ isLoading: true, error: null });
    try {
      const profile = await profileApi.updateProfile(request);
      set({ profile, isLoading: false });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte uppdatera profil';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  addSkill: async (request: AddSkillRequest) => {
    set({ isLoading: true, error: null });
    try {
      const skill = await profileApi.addSkill(request);
      const currentProfile = get().profile;
      if (currentProfile) {
        set({
          profile: {
            ...currentProfile,
            skills: [...currentProfile.skills, skill],
          },
          isLoading: false,
        });
      }
      return skill;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte lägga till kompetens';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  removeSkill: async (skillId: string) => {
    set({ isLoading: true, error: null });
    try {
      await profileApi.removeSkill(skillId);
      const currentProfile = get().profile;
      if (currentProfile) {
        set({
          profile: {
            ...currentProfile,
            skills: currentProfile.skills.filter((s) => s.id !== skillId),
          },
          isLoading: false,
        });
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte ta bort kompetens';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  updatePreferences: async (request: UpdateJobPreferenceRequest) => {
    set({ isLoading: true, error: null });
    try {
      const jobPreference = await profileApi.updatePreferences(request);
      const currentProfile = get().profile;
      if (currentProfile) {
        set({
          profile: {
            ...currentProfile,
            jobPreference,
          },
          isLoading: false,
        });
      }
      return jobPreference;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte uppdatera inställningar';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  uploadCV: async (file: File) => {
    set({ isLoading: true, error: null });
    try {
      const cvDocument = await profileApi.uploadCV(file);
      const currentProfile = get().profile;
      if (currentProfile) {
        set({
          profile: {
            ...currentProfile,
            cvDocuments: [...currentProfile.cvDocuments, cvDocument],
          },
          isLoading: false,
        });
      }
      return cvDocument;
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte ladda upp CV';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  deleteCV: async (documentId: string) => {
    set({ isLoading: true, error: null });
    try {
      await profileApi.deleteCV(documentId);
      const currentProfile = get().profile;
      if (currentProfile) {
        const updatedDocs = currentProfile.cvDocuments.filter((d) => d.id !== documentId);
        // If deleted doc was primary, make first remaining doc primary
        if (
          updatedDocs.length > 0 &&
          currentProfile.cvDocuments.find((d) => d.id === documentId)?.isPrimary
        ) {
          updatedDocs[0].isPrimary = true;
        }
        set({
          profile: {
            ...currentProfile,
            cvDocuments: updatedDocs,
          },
          isLoading: false,
        });
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte ta bort CV';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  setPrimaryCV: async (documentId: string) => {
    set({ isLoading: true, error: null });
    try {
      await profileApi.setPrimaryCV(documentId);
      const currentProfile = get().profile;
      if (currentProfile) {
        set({
          profile: {
            ...currentProfile,
            cvDocuments: currentProfile.cvDocuments.map((d) => ({
              ...d,
              isPrimary: d.id === documentId,
            })),
          },
          isLoading: false,
        });
      }
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Kunde inte sätta primärt CV';
      set({ error: message, isLoading: false });
      throw error;
    }
  },

  clearError: () => set({ error: null }),

  reset: () => set({ profile: null, isLoading: false, error: null }),
}));
