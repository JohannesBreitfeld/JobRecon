import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Link,
  Box,
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
    <Container maxWidth="sm">
      <Box
        sx={{
          mt: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Paper elevation={3} sx={{ p: 4, width: '100%' }}>
          <Typography component="h1" variant="h4" align="center" gutterBottom>
            Logga in
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center" sx={{ mb: 3 }}>
            Välkommen tillbaka! Logga in på ditt konto.
          </Typography>

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
  );
}
