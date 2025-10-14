import { PostsList } from '@entities';
import style from './MainPage.module.css';

const MainPage = (): React.JSX.Element => {
  return (
    <>
      <h1 className={style.MainPage__title}>Main Page</h1>
      <PostsList />
    </>
  );
}

export default MainPage;
