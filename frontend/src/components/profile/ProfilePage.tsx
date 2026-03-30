import {
  Container,
  Paper,
  Typography,
  Box,
  Alert,
  CircularProgress,
  Button,
} from '@mui/material';
import { useProfile, useCreateProfile } from '../../api/hooks/useProfile';
import { ApiError } from '../../api/client';
import { ProfileForm } from './ProfileForm';
import { SkillsSection } from './SkillsSection';
import { PreferencesSection } from './PreferencesSection';

export function ProfilePage() {
  const { data: profile, isLoading, error } = useProfile();
  const createProfileMutation = useCreateProfile();

  const profileNotFound = error instanceof ApiError && error.code === 'Profile.NotFound';

  const handleCreateProfile = () => {
    createProfileMutation.mutate({});
  };

  if (isLoading) {
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
            disabled={createProfileMutation.isPending}
          >
            {createProfileMutation.isPending ? 'Skapar profil...' : 'Skapa min profil'}
          </Button>
        </Paper>
      </Container>
    );
  }

  if (error && !profileNotFound) {
    return (
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Alert severity="error">
          {error instanceof Error ? error.message : 'Kunde inte hämta profil'}
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
