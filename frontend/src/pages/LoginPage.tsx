import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Link,
  Box,
  alpha,
} from '@mui/material';
import { LoginForm } from '../components/auth/LoginForm';

export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || '/';

  const handleSuccess = () => {
    navigate(from, { replace: true });
  };

  return (
    <Box
      sx={{
        minHeight: 'calc(100vh - 64px)',
        display: 'flex',
        alignItems: 'center',
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
                Logga in
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Välkommen tillbaka! Logga in på ditt konto.
              </Typography>
            </Box>

            <LoginForm onSuccess={handleSuccess} />

            <Box sx={{ mt: 3, textAlign: 'center' }}>
              <Typography variant="body2">
                Har du inget konto?{' '}
                <Link component={RouterLink} to="/register">
                  Skapa ett
                </Link>
              </Typography>
            </Box>
          </Paper>
        </Box>
      </Container>
    </Box>
  );
}
