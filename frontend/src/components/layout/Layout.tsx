import { Box, useMediaQuery, useTheme } from '@mui/material';
import { Navbar } from './Navbar';
import { Footer } from './Footer';
import { BottomNav } from './BottomNav';

interface LayoutProps {
  children: React.ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <Navbar />
      <Box component="main" sx={{ flexGrow: 1, pb: isMobile ? '56px' : 0 }}>
        {children}
      </Box>
      {isMobile ? <BottomNav /> : <Footer />}
    </Box>
  );
}
