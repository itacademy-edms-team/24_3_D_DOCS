import cn from '@bem';

import { usePost } from '../model/usePost';
import styles from './Posts.module.css';

const bem = cn('PostsList');

export const PostsList = (): React.JSX.Element => {
	const {
		posts
	} = usePost();

	return (
		<div className={styles[bem()]}>
			{posts?.map((item) => (
				<div key={`${item.id}`} className={styles[bem('card')]}>
					{item.title}
				</div>
			))}
		</div>
	);
};
