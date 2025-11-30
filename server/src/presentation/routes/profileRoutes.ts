import { Router, Request, Response } from 'express';
import type { ProfileService } from '../../application/services/ProfileService';

/**
 * Create Profile routes with injected service
 */
export function createProfileRoutes(profileService: ProfileService): Router {
  const router = Router();

  // GET /api/profiles - Get all profiles
  router.get('/', async (req: Request, res: Response) => {
    try {
      const profiles = await profileService.getAllProfiles();
      res.json(profiles);
    } catch (error) {
      console.error('Failed to get profiles:', error);
      res.status(500).json({ error: 'Failed to get profiles' });
    }
  });

  // GET /api/profiles/:id - Get profile by ID
  router.get('/:id', async (req: Request, res: Response) => {
    try {
      const profile = await profileService.getProfileById(req.params.id);
      
      if (!profile) {
        return res.status(404).json({ error: 'Profile not found' });
      }
      
      res.json(profile);
    } catch (error) {
      console.error('Failed to get profile:', error);
      res.status(500).json({ error: 'Failed to get profile' });
    }
  });

  // POST /api/profiles - Create new profile
  router.post('/', async (req: Request, res: Response) => {
    try {
      const { name } = req.body;
      const profile = await profileService.createProfile({ name });
      res.status(201).json(profile);
    } catch (error) {
      console.error('Failed to create profile:', error);
      res.status(500).json({ error: 'Failed to create profile' });
    }
  });

  // PUT /api/profiles/:id - Update profile
  router.put('/:id', async (req: Request, res: Response) => {
    try {
      const { name, page, entities } = req.body;
      const profile = await profileService.updateProfile(req.params.id, { name, page, entities });
      
      if (!profile) {
        return res.status(404).json({ error: 'Profile not found' });
      }
      
      res.json(profile);
    } catch (error) {
      console.error('Failed to update profile:', error);
      res.status(500).json({ error: 'Failed to update profile' });
    }
  });

  // DELETE /api/profiles/:id - Delete profile
  router.delete('/:id', async (req: Request, res: Response) => {
    try {
      const deleted = await profileService.deleteProfile(req.params.id);
      
      if (!deleted) {
        return res.status(404).json({ error: 'Profile not found' });
      }
      
      res.json({ success: true });
    } catch (error) {
      console.error('Failed to delete profile:', error);
      res.status(500).json({ error: 'Failed to delete profile' });
    }
  });

  return router;
}

