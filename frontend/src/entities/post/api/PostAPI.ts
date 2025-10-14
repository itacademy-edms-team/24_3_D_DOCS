import HttpClient from '@api';

import { PostsSchema, type Posts } from '../type';

interface IVerselAPI {
  getPosts: () => void;
}

class PostAPI extends HttpClient implements IVerselAPI {
  public async getPosts(): Promise<Posts> {
    const data = await this._get<Posts>('/blog');
    return PostsSchema.parse(data);
  }
}

export default PostAPI;
