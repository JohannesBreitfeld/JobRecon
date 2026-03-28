import { useEffect } from 'react';
import {
  Container,
  Paper,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Button,
} from '@mui/material';
import { useState } from 'react';
import { useProfileStore } from '../../stores/profileStore';
import { ProfileForm } from './ProfileForm';
import { SkillsSection } from './SkillsSection';
import { PreferencesSection } from './PreferencesSection';

export function ProfilePage() {
  const { profile, isLoading, error, profileNotFound, fetchProfile, createProfile, clearError } = useProfileStore();
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  const handleCreateProfile = async () => {
    setCreating(true);
    try {
      await createProfile({});
    } finally {
      setCreating(false);
    }
  };

  if (isLoading && !profile) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (!profile && profileNotFound) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography variant="h5" gutterBottom>
            Välkommen till JobRecon!
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            Du har inte skapat en profil ännu. Skapa en profil för att börja matcha med jobb.
          </Typography>
          <Button
            variant="contained"
            size="large"
            onClick={handleCreateProfile}
            disabled={creating}
          >
            {creating ? 'Skapar profil...' : 'Skapa min profil'}
          </Button>
        </Paper>
      </Container>
    );
  }

  if (error) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Alert severity="error" onClose={clearError}>
          {error}
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>
        Min profil
      </Typography>

      <Paper sx={{ p: 3, mt: 2 }}>
        <ProfileForm />
      </Paper>

      <Paper sx={{ p: 3, mt: 3 }}>
        <SkillsSection />
      </Paper>

      <Paper sx={{ p: 3, mt: 3 }}>
        <PreferencesSection />
      </Paper>
    </Container>
  );
}
