import { useNavigate, useLocation } from 'react-router-dom';
import { BottomNavigation, BottomNavigationAction, Paper } from '@mui/material';
import {
  Work as WorkIcon,
  AutoAwesome as AutoAwesomeIcon,
  Person as PersonIcon,
} from '@mui/icons-material';
import { useAuthStore } from '../../stores/authStore';

export function BottomNav() {
  const navigate = useNavigate();
  const location = useLocation();
  const { isAuthenticated } = useAuthStore();

  const routes = [
    { label: 'Jobb', icon: <WorkIcon />, path: '/jobs' },
    ...(isAuthenticated
      ? [
          { label: 'Rekommendationer', icon: <AutoAwesomeIcon />, path: '/recommendations' },
          { label: 'Profil', icon: <PersonIcon />, path: '/profile' },
        ]
      : []),
  ];

  const currentIndex = routes.findIndex((r) => location.pathname.startsWith(r.path));

  return (
    <Paper
      sx={{ position: 'fixed', bottom: 0, left: 0, right: 0, zIndex: 1100 }}
      elevation={8}
    >
      <BottomNavigation
        value={currentIndex >= 0 ? currentIndex : -1}
        onChange={(_, newValue) => {
          if (routes[newValue]) navigate(routes[newValue].path);
        }}
        showLabels
      >
        {routes.map((route) => (
          <BottomNavigationAction
            key={route.path}
            label={route.label}
            icon={route.icon}
          />
        ))}
      </BottomNavigation>
    </Paper>
  );
}
