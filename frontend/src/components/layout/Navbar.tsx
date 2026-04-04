import { Link as RouterLink, useNavigate, useLocation } from 'react-router-dom';
import {
  AppBar,
  Toolbar,
  Button,
  Box,
  IconButton,
  Menu,
  MenuItem,
  Avatar,
  Badge,
  Divider,
  ListItemIcon,
  ListItemText,
  Popover,
  Typography,
  useMediaQuery,
  useTheme,
  Drawer,
  List,
  ListItemButton,
} from '@mui/material';
import {
  AccountCircle,
  Person as PersonIcon,
  Logout as LogoutIcon,
  Menu as MenuIcon,
  Work as WorkIcon,
  AutoAwesome as AutoAwesomeIcon,
  NotificationsOutlined as NotificationsIcon,
} from '@mui/icons-material';
import { useState } from 'react';
import { useAuthStore } from '../../stores/authStore';

export function Navbar() {
  const { isAuthenticated, user, logout } = useAuthStore();
  const navigate = useNavigate();
  const location = useLocation();
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('md'));

  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [notifAnchor, setNotifAnchor] = useState<null | HTMLElement>(null);
  const [mobileOpen, setMobileOpen] = useState(false);

  const handleMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = async () => {
    handleClose();
    await logout();
    navigate('/');
  };

  const isActive = (path: string) => location.pathname === path;

  const navLinkSx = (path: string) => ({
    color: 'inherit',
    position: 'relative' as const,
    borderRadius: 1,
    px: 2,
    '&::after': {
      content: '""',
      position: 'absolute' as const,
      bottom: 0,
      left: '50%',
      transform: isActive(path) ? 'translateX(-50%) scaleX(1)' : 'translateX(-50%) scaleX(0)',
      width: '70%',
      height: 2,
      backgroundColor: '#5ba532',
      borderRadius: 1,
      transition: 'transform 0.2s ease',
    },
    '&:hover::after': {
      transform: 'translateX(-50%) scaleX(1)',
    },
  });

  const mobileDrawer = (
    <Drawer
      anchor="left"
      open={mobileOpen}
      onClose={() => setMobileOpen(false)}
      PaperProps={{ sx: { width: 260 } }}
    >
      <Box sx={{ p: 2 }}>
        <Box
          component="img"
          src="/images/logo-web.png"
          alt="JobRecon"
          sx={{ height: 32, mb: 2 }}
        />
      </Box>
      <Divider />
      <List>
        <ListItemButton
          component={RouterLink}
          to="/jobs"
          selected={isActive('/jobs')}
          onClick={() => setMobileOpen(false)}
        >
          <ListItemIcon><WorkIcon /></ListItemIcon>
          <ListItemText primary="Jobb" />
        </ListItemButton>
        {isAuthenticated && (
          <ListItemButton
            component={RouterLink}
            to="/recommendations"
            selected={isActive('/recommendations')}
            onClick={() => setMobileOpen(false)}
          >
            <ListItemIcon><AutoAwesomeIcon /></ListItemIcon>
            <ListItemText primary="Rekommendationer" />
          </ListItemButton>
        )}
        {isAuthenticated && (
          <ListItemButton
            component={RouterLink}
            to="/profile"
            selected={isActive('/profile')}
            onClick={() => setMobileOpen(false)}
          >
            <ListItemIcon><PersonIcon /></ListItemIcon>
            <ListItemText primary="Profil" />
          </ListItemButton>
        )}
      </List>
      <Divider />
      <List>
        {isAuthenticated ? (
          <ListItemButton onClick={() => { setMobileOpen(false); handleLogout(); }}>
            <ListItemIcon><LogoutIcon /></ListItemIcon>
            <ListItemText primary="Logga ut" />
          </ListItemButton>
        ) : (
          <>
            <ListItemButton
              component={RouterLink}
              to="/login"
              onClick={() => setMobileOpen(false)}
            >
              <ListItemText primary="Logga in" />
            </ListItemButton>
            <ListItemButton
              component={RouterLink}
              to="/register"
              onClick={() => setMobileOpen(false)}
            >
              <ListItemText primary="Registrera" />
            </ListItemButton>
          </>
        )}
      </List>
    </Drawer>
  );

  return (
    <>
      <AppBar position="sticky" elevation={0} sx={{ borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
        <Toolbar sx={{ gap: 1 }}>
          {isMobile && (
            <IconButton
              color="inherit"
              edge="start"
              onClick={() => setMobileOpen(true)}
              aria-label="Meny"
            >
              <MenuIcon />
            </IconButton>
          )}

          <Box
            component={RouterLink}
            to="/"
            sx={{
              display: 'flex',
              alignItems: 'center',
              textDecoration: 'none',
            }}
          >
            <Box
              component="img"
              src="/images/logo-web.png"
              alt="JobRecon"
              sx={{
                height: 36,
                filter: 'brightness(1.8) saturate(0.8)',
              }}
            />
          </Box>

          {!isMobile && (
            <Box sx={{ display: 'flex', gap: 0.5, flexGrow: 1, ml: 3 }}>
              <Button sx={navLinkSx('/jobs')} component={RouterLink} to="/jobs">
                Jobb
              </Button>
              {isAuthenticated && (
                <Button sx={navLinkSx('/recommendations')} component={RouterLink} to="/recommendations">
                  Rekommendationer
                </Button>
              )}
            </Box>
          )}

          {isMobile && <Box sx={{ flexGrow: 1 }} />}

          {!isMobile && !isAuthenticated && <Box sx={{ flexGrow: 1 }} />}

          {isAuthenticated ? (
            <Box sx={{ display: 'flex', alignItems: 'center' }}>
              <IconButton
                color="inherit"
                aria-label="Notifikationer"
                onClick={(e) => setNotifAnchor(e.currentTarget)}
              >
                <Badge variant="dot" color="secondary" invisible>
                  <NotificationsIcon />
                </Badge>
              </IconButton>
              <Popover
                open={Boolean(notifAnchor)}
                anchorEl={notifAnchor}
                onClose={() => setNotifAnchor(null)}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
                slotProps={{ paper: { sx: { p: 2, minWidth: 240, mt: 1 } } }}
              >
                <Typography variant="subtitle2" gutterBottom>
                  Notifikationer
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  Inga nya notifikationer
                </Typography>
              </Popover>
              <IconButton
                size="large"
                onClick={handleMenu}
                color="inherit"
                aria-label="Kontomeny"
                aria-controls={anchorEl ? 'account-menu' : undefined}
                aria-haspopup="true"
                aria-expanded={Boolean(anchorEl)}
              >
                {user?.firstName ? (
                  <Avatar
                    sx={{
                      width: 34,
                      height: 34,
                      bgcolor: 'secondary.main',
                      fontSize: '0.95rem',
                      fontWeight: 600,
                    }}
                  >
                    {user.firstName[0].toUpperCase()}
                  </Avatar>
                ) : (
                  <AccountCircle />
                )}
              </IconButton>
              <Menu
                id="account-menu"
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleClose}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                transformOrigin={{ vertical: 'top', horizontal: 'right' }}
                slotProps={{
                  paper: {
                    sx: { minWidth: 200, mt: 1 },
                  },
                }}
              >
                <MenuItem disabled sx={{ opacity: '0.7 !important' }}>
                  {user?.email}
                </MenuItem>
                <Divider />
                <MenuItem onClick={handleClose} component={RouterLink} to="/profile">
                  <ListItemIcon><PersonIcon fontSize="small" /></ListItemIcon>
                  <ListItemText>Profil</ListItemText>
                </MenuItem>
                <MenuItem onClick={handleLogout}>
                  <ListItemIcon><LogoutIcon fontSize="small" /></ListItemIcon>
                  <ListItemText>Logga ut</ListItemText>
                </MenuItem>
              </Menu>
            </Box>
          ) : (
            !isMobile && (
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Button
                  color="inherit"
                  component={RouterLink}
                  to="/login"
                >
                  Logga in
                </Button>
                <Button
                  variant="contained"
                  color="secondary"
                  component={RouterLink}
                  to="/register"
                >
                  Registrera
                </Button>
              </Box>
            )
          )}
        </Toolbar>
      </AppBar>
      {mobileDrawer}
    </>
  );
}
