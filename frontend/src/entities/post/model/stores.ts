import { create, createSelectors } from '@model';

import type { Posts } from '../type';
import PostAPI from '../api/PostAPI';

const api = new PostAPI();

type State = {
  posts: Posts | undefined;
}

type Actions = {
  getPosts: () => Promise<void>;
  removePosts: () => void;
}

type Store = State & Actions;

const usePostsStoreBase = create<Store>()((set) => ({
  posts: undefined,
  
  getPosts: async () => set({ posts: await api.getPosts() }),

  removePosts: () => set({ posts: undefined }),
}));

export const usePostsStore = createSelectors(usePostsStoreBase);
