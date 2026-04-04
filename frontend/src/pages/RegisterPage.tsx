import { Link as RouterLink, useNavigate } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Link,
  Box,
  alpha,
} from '@mui/material';
import { RegisterForm } from '../components/auth/RegisterForm';

export function RegisterPage() {
  const navigate = useNavigate();

  const handleSuccess = () => {
    navigate('/profile', { replace: true });
  };

  return (
    <Box
      sx={{
        minHeight: 'calc(100vh - 64px)',
        display: 'flex',
        alignItems: 'center',
        py: 4,
        background: (theme) =>
          `linear-gradient(160deg, ${alpha(theme.palette.primary.dark, 0.03)} 0%, ${alpha(theme.palette.secondary.main, 0.04)} 100%)`,
      }}
    >
      <Container maxWidth="sm">
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          <Paper
            elevation={0}
            sx={{
              p: { xs: 3, sm: 5 },
              width: '100%',
              border: 1,
              borderColor: 'divider',
            }}
          >
            <Box sx={{ textAlign: 'center', mb: 3 }}>
              <Box
                component="img"
                src="/images/logo-web.png"
                alt="JobRecon"
                sx={{ height: 40, mb: 2 }}
              />
              <Typography component="h1" variant="h4" gutterBottom>
                Skapa konto
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Gå med i JobRecon för att hitta ditt perfekta jobb.
              </Typography>
            </Box>

            <RegisterForm onSuccess={handleSuccess} />

            <Box sx={{ mt: 3, textAlign: 'center' }}>
              <Typography variant="body2">
                Har du redan ett konto?{' '}
                <Link component={RouterLink} to="/login">
                  Logga in
                </Link>
              </Typography>
            </Box>
          </Paper>
        </Box>
      </Container>
    </Box>
  );
}
