import { useEffect } from "react"
import { usePostsStore } from "./stores"

export const usePost = () => {
  const posts = usePostsStore.use.posts();
  const getPosts = usePostsStore.use.getPosts();
  const removePosts = usePostsStore.use.removePosts();

  useEffect(() => {
    getPosts();
  }, [])

  return {
    posts,
    getPosts,
    removePosts,
  }
}
