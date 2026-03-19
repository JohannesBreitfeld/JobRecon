import { useEffect } from 'react';
import {
  Container,
  Paper,
  Typography,
  Box,
  Tabs,
  Tab,
  Alert,
  CircularProgress,
  Button,
} from '@mui/material';
import { useState } from 'react';
import { useProfileStore } from '../../stores/profileStore';
import { ProfileForm } from './ProfileForm';
import { SkillsSection } from './SkillsSection';
import { PreferencesSection } from './PreferencesSection';
import { CVSection } from './CVSection';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

function TabPanel(props: TabPanelProps) {
  const { children, value, index, ...other } = props;

  return (
    <div
      role="tabpanel"
      hidden={value !== index}
      id={`profile-tabpanel-${index}`}
      aria-labelledby={`profile-tab-${index}`}
      {...other}
    >
      {value === index && <Box sx={{ py: 3 }}>{children}</Box>}
    </div>
  );
}

export function ProfilePage() {
  const { profile, isLoading, error, fetchProfile, createProfile, clearError } = useProfileStore();
  const [tabValue, setTabValue] = useState(0);
  const [creating, setCreating] = useState(false);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  const handleTabChange = (_: React.SyntheticEvent, newValue: number) => {
    setTabValue(newValue);
  };

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
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  // Profile doesn't exist yet - show create option
  if (!profile && error?.includes('not found')) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
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

  if (error && !error.includes('not found')) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Alert severity="error" onClose={clearError}>
          {error}
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" gutterBottom>
        Min profil
      </Typography>

      {error && (
        <Alert severity="error" onClose={clearError} sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Paper sx={{ mt: 2 }}>
        <Tabs
          value={tabValue}
          onChange={handleTabChange}
          aria-label="profile tabs"
          sx={{ borderBottom: 1, borderColor: 'divider' }}
        >
          <Tab label="Grundinfo" id="profile-tab-0" aria-controls="profile-tabpanel-0" />
          <Tab label="Kompetenser" id="profile-tab-1" aria-controls="profile-tabpanel-1" />
          <Tab label="Preferenser" id="profile-tab-2" aria-controls="profile-tabpanel-2" />
          <Tab label="CV" id="profile-tab-3" aria-controls="profile-tabpanel-3" />
        </Tabs>

        <Box sx={{ p: 3 }}>
          <TabPanel value={tabValue} index={0}>
            <ProfileForm />
          </TabPanel>
          <TabPanel value={tabValue} index={1}>
            <SkillsSection />
          </TabPanel>
          <TabPanel value={tabValue} index={2}>
            <PreferencesSection />
          </TabPanel>
          <TabPanel value={tabValue} index={3}>
            <CVSection />
          </TabPanel>
        </Box>
      </Paper>
    </Container>
  );
}
