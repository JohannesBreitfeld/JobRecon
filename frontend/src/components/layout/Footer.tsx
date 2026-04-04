import { Box, Container, Typography, Link, Divider } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { useAuthStore } from '../../stores/authStore';

export function Footer() {
  const { isAuthenticated } = useAuthStore();

  return (
    <Box
      component="footer"
      sx={{
        py: 3,
        px: 2,
        mt: 'auto',
        borderTop: 1,
        borderColor: 'divider',
        backgroundColor: 'background.paper',
      }}
    >
      <Container maxWidth="lg">
        <Box
          sx={{
            display: 'flex',
            flexDirection: { xs: 'column', sm: 'row' },
            justifyContent: 'space-between',
            alignItems: 'center',
            gap: 1.5,
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
            <Box
              component="img"
              src="/images/logo-web.png"
              alt="JobRecon"
              sx={{ height: 24, opacity: 0.8 }}
            />
            <Typography variant="body2" color="text.secondary">
              AI-driven jobbsökning
            </Typography>
          </Box>
          <Box sx={{ display: 'flex', gap: 3 }}>
            <Link component={RouterLink} to="/jobs" color="text.secondary" underline="hover" variant="body2">
              Jobb
            </Link>
            {isAuthenticated ? (
              <Link component={RouterLink} to="/profile" color="text.secondary" underline="hover" variant="body2">
                Profil
              </Link>
            ) : (
              <Link component={RouterLink} to="/login" color="text.secondary" underline="hover" variant="body2">
                Logga in
              </Link>
            )}
          </Box>
          <Typography variant="caption" color="text.secondary">
            &copy; {new Date().getFullYear()} JobRecon
          </Typography>
        </Box>
        <Divider sx={{ my: 1.5 }} />
        <Box
          sx={{
            display: 'flex',
            justifyContent: 'center',
            gap: 3,
            flexWrap: 'wrap',
          }}
        >
          <Link href="#" color="text.secondary" underline="hover" variant="caption">
            Om oss
          </Link>
          <Link href="#" color="text.secondary" underline="hover" variant="caption">
            Kontakt
          </Link>
          <Link href="#" color="text.secondary" underline="hover" variant="caption">
            Integritetspolicy
          </Link>
          <Link href="#" color="text.secondary" underline="hover" variant="caption">
            Villkor
          </Link>
        </Box>
      </Container>
    </Box>
  );
}
