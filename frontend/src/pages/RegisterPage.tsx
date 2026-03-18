import { Link as RouterLink, useNavigate } from 'react-router-dom';
import {
  Container,
  Paper,
  Typography,
  Link,
  Box,
} from '@mui/material';
import { RegisterForm } from '../components/auth/RegisterForm';

export function RegisterPage() {
  const navigate = useNavigate();

  const handleSuccess = () => {
    navigate('/', { replace: true });
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
            Skapa konto
          </Typography>

          <Typography variant="body2" color="text.secondary" align="center" sx={{ mb: 3 }}>
            Gå med i JobRecon för att hitta ditt perfekta jobb.
          </Typography>

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
  );
}
