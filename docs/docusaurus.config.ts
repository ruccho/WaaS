import { themes as prismThemes } from 'prism-react-renderer';
import type { Config } from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'WaaS',
  tagline: 'A language-independent scripting engine for Unity and .NET using WebAssembly.',
  favicon: 'img/favicon.ico',
  url: 'https://ruccho.com/',
  baseUrl: '/WaaS/',
  organizationName: 'ruccho',
  projectName: 'WaaS',
  trailingSlash: false,

  onBrokenLinks: 'throw',

  i18n: {
    defaultLocale: 'en',
    locales: ['en', 'ja'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          editUrl:
            'https://github.com/ruccho/WaaS/tree/main/docs',
          routeBasePath: '/'
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],
  plugins: [
    [
      '@docusaurus/plugin-content-docs',
      {
        id: 'api',
        path: 'api',
        routeBasePath: 'api',
        sidebarPath: './sidebars.ts',
      },
    ],
  ],

  themeConfig: {
    image: 'img/social1.png',
    navbar: {
      title: 'WaaS',
      logo: {
        alt: 'WaaS Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'localeDropdown',
          position: 'left'
        },
        {
          to: '/',
          label: 'Docs',
          position: 'left',
          className: 'header-link',
        },
        {
          to: 'api',
          label: 'API',
          position: 'left',
          className: 'header-link',
        },
        {
          href: 'https://github.com/ruccho/WaaS',
          position: 'right',
          className: 'header-github-link',
        },
      ],
      hideOnScroll: true
    },
    footer: {
      style: 'light',
      copyright: `© ${new Date().getFullYear()} ruccho`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'bash', 'toml'],
    },
    colorMode: {
      defaultMode: 'dark',
    },
  } satisfies Preset.ThemeConfig,
  markdown: {
    mermaid: true,
    hooks: {
      onBrokenMarkdownLinks: 'warn',
    },
  },
  themes: ['@docusaurus/theme-mermaid'],
};

export default config;
