import React from 'react';
import clsx from 'clsx';
import { ThemeClassNames } from '@docusaurus/theme-common';

export default function Badge({ children, className }: {
    children?: React.ReactNode;
    className?: string
}) {
    return (
        <>
            <span
                className={clsx(
                    className,
                    ThemeClassNames.docs.docVersionBadge,
                    'badge badge--secondary',
                )}>
                {children}
            </span>
        </>
    )
}