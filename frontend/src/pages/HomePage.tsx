import { Container, Typography, Box, Button, Paper, Stack } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';

export function HomePage() {
  const { isAuthenticated, user } = useAuthStore();

  return (
    <Container maxWidth="lg">
      <Box sx={{ textAlign: 'center', mt: 8, mb: 6 }}>
        <Typography variant="h2" component="h1" gutterBottom>
          Hitta ditt perfekta jobb
        </Typography>
        <Typography variant="h5" color="text.secondary" sx={{ mb: 4 }}>
          AI-driven jobbmatchning som förstår dina färdigheter, erfarenhet och karriärmål
        </Typography>

        {isAuthenticated ? (
          <Box>
            <Typography variant="h6" sx={{ mb: 2 }}>
              Välkommen tillbaka, {user?.firstName || user?.email}!
            </Typography>
            <Button
              variant="contained"
              size="large"
              component={RouterLink}
              to="/jobs"
              sx={{ mr: 2 }}
            >
              Bläddra bland jobb
            </Button>
            <Button
              variant="outlined"
              size="large"
              component={RouterLink}
              to="/profile"
            >
              Visa profil
            </Button>
          </Box>
        ) : (
          <Box>
            <Button
              variant="contained"
              size="large"
              component={RouterLink}
              to="/register"
              sx={{ mr: 2 }}
            >
              Kom igång
            </Button>
            <Button
              variant="outlined"
              size="large"
              component={RouterLink}
              to="/login"
            >
              Logga in
            </Button>
          </Box>
        )}
      </Box>

      <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
        <Paper sx={{ p: 3, flex: 1 }}>
          <Typography variant="h5" gutterBottom>
            Smart matchning
          </Typography>
          <Typography color="text.secondary">
            Vår AI analyserar din profil och hittar jobb som verkligen matchar dina färdigheter och erfarenhet.
          </Typography>
        </Paper>
        <Paper sx={{ p: 3, flex: 1 }}>
          <Typography variant="h5" gutterBottom>
            Realtidsuppdateringar
          </Typography>
          <Typography color="text.secondary">
            Få meddelanden direkt när nya jobb som matchar din profil publiceras.
          </Typography>
        </Paper>
        <Paper sx={{ p: 3, flex: 1 }}>
          <Typography variant="h5" gutterBottom>
            Karriärinsikter
          </Typography>
          <Typography color="text.secondary">
            Förstå ditt marknadsvärde och upptäck möjligheter för tillväxt.
          </Typography>
        </Paper>
      </Stack>
    </Container>
  );
}
