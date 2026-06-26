'use client';

import Image from 'next/image';
import Link from 'next/link';

export function SiteFooter() {
  return (
    <footer className="bg-[#023059] pt-5">
      <div className="container mt-5 flex flex-col justify-center pt-5">
        <div className="container my-5 grid grid-cols-12 gap-4 py-5">
          <div className="col-span-3">
            <Link href="/" className="py-5">
              <Image
                src="/images/GeekAtYourSpotWhite.svg"
                alt="Geek @ Your Spot"
                width={116}
                height={48}
                priority
              />
            </Link>
            <p className="text-md py-5 text-white shadow-text">
              Design and build AI implementation and automation architectures for brands that refuse
              to settle for average.
            </p>
            <div className="flex">
              <span className="pe-5">
                <a
                  target="_blank"
                  rel="noreferrer"
                  className="group inline-block h-8 w-8 cursor-pointer items-center justify-center transition-all duration-300"
                  href="https://www.facebook.com/GeekAtYourSpot/"
                >
                  <svg
                    className="h-8 w-8 transition-transform group-hover:scale-110"
                    fill="#ff0000"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M22 12c0-5.523-4.477-10-10-10S2 6.477 2 12c0 4.991 3.657 9.128 8.438 9.878v-6.987h-2.54V12h2.54V9.797c0-2.506 1.492-3.89 3.777-3.89 1.094 0 2.238.195 2.238.195v2.46h-1.26c-1.243 0-1.63.771-1.63 1.562V12h2.773l-.443 2.89h-2.33v6.988C18.343 21.128 22 16.991 22 12z"
                      clipRule="evenodd"
                    />
                  </svg>
                </a>
              </span>
              <span className="px-5">
                <a
                  target="_blank"
                  rel="noreferrer"
                  className="group inline-block h-8 w-8 cursor-pointer items-center justify-center transition-all duration-300"
                  href="https://www.linkedin.com/company/geekatyourspot"
                >
                  <svg
                    className="h-7 w-8 transition-transform group-hover:scale-110"
                    fill="#ff0000"
                    viewBox="0 0 24 24"
                    aria-hidden="true"
                  >
                    <path
                      fillRule="evenodd"
                      d="M19 0h-14c-2.761 0-5 2.239-5 5v14c0 2.761 2.239 5 5 5h14c2.762 0 5-2.239 5-5v-14c0-2.761-2.238-5-5-5zm-11 19h-3v-11h3v11zm-1.5-12.268c-.966 0-1.75-.79-1.75-1.764s.784-1.764 1.75-1.764 1.75.79 1.75 1.764-.783 1.764-1.75 1.764zm13.5 12.268h-3v-5.604c0-3.368-4-3.113-4 0v5.604h-3v-11h3v1.765c1.396-2.586 7-2.777 7 2.476v6.759z"
                      clipRule="evenodd"
                    />
                  </svg>
                </a>
              </span>
            </div>
          </div>
          <div className="col-span-6">&nbsp;</div>
          <div className="col-span-3">
            <h2 className="text-md font-bold uppercase text-white shadow-text">Contact</h2>
            <ul>
              <li className="py-1">
                <a href="mailto:info@geekatyourspot.com" className="text-sm text-white shadow-text">
                  <span className="text-sm font-bold text-white shadow-text">EMail</span>
                  <br />
                  info@geekatyourspot.com
                </a>
              </li>
              <li className="py-2">
                <a href="tel:+15615263512" className="text-sm text-white shadow-text">
                  <span className="text-sm font-bold text-white shadow-text">Call Us</span>
                  <br />
                  (561) 526-3512
                </a>
              </li>
              <li className="py-2">
                <a
                  href="https://share.google/N5czXCIcHvptENeqr"
                  className="text-sm text-white shadow-text"
                >
                  <span className="text-sm font-bold text-white shadow-text">Headquarters</span>
                  <br />
                  Delray Beach, Fl
                </a>
              </li>
            </ul>
          </div>
        </div>
        <div className="mt-5 flex flex-col items-center justify-center border-t border-white/5 py-5">
          <div className="flex w-full flex-row items-center justify-between gap-6 px-5 text-center text-xs uppercase tracking-widest text-slate-500">
            <p className="inline-block text-center">© 2026 Geek at Your Spot</p>
            <p className="inline-block text-center">All rights reserved</p>
            <p className="inline-block text-center">
              <a className="transition-colors hover:text-white" href="/privacy-policy">
                Privacy Policy
              </a>
            </p>
            <p className="inline-block text-center">
              <a className="transition-colors hover:text-white" href="/terms-and-conditions">
                Terms of Service
              </a>
            </p>
          </div>
        </div>
      </div>
    </footer>
  );
}
