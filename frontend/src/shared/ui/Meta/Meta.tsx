import { Helmet } from "react-helmet-async";

type MetaProps = {
  lang: string;
  title: string;
  description: string;
}

export const Meta = ({
  lang,
  title,
  description,
}: MetaProps): React.JSX.Element => (
  <Helmet htmlAttributes={{ lang }}>
    <title>{title}</title>
    <meta name='description' content={description} />
		{/* <link rel='preload' href='favicon.ico' as='image' />
		<link rel='icon' href='favicon.ico' sizes='any' />
		<link rel='icon' href='favicon.svg' type='image/svg+xml' />
		<link rel='apple-touch-icon' href='apple-touch-icon.png' /> */}
	</Helmet>
)
